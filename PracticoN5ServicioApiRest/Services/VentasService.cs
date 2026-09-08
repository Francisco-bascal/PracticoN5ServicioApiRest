using Microsoft.EntityFrameworkCore;
using PracticoN5ServicioApiRest.Data;
using PracticoN5ServicioApiRest.DTOs;
using PracticoN5ServicioApiRest.Models;

namespace PracticoN5ServicioApiRest.Services
{
    public class VentasService
    {
        private readonly SistemaVentasDbContext _contexto;

        public VentasService(SistemaVentasDbContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<ResultadoPaginadoDto<Venta>> ObtenerTodosAsync(
            int pagina = 1, 
            int tamanoPagina = 10, 
            CancellationToken cancellationToken = default)
        {
            if (pagina <= 0)
            {
                throw new ArgumentException("El número de página debe ser mayor o igual a 1.", nameof(pagina));
            }

            if (tamanoPagina <= 0)
            {
                throw new ArgumentException("El tamaño de página debe ser mayor a 0.", nameof(tamanoPagina));
            }

            if (tamanoPagina > 100)
            {
                throw new ArgumentException("El tamaño de página no puede superar el límite máximo de 100 elementos.", nameof(tamanoPagina));
            }

            var consulta = _contexto.Ventas
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto);

            int totalElementos = await consulta.CountAsync(cancellationToken);
            int totalPaginas = (int)Math.Ceiling(totalElementos / (double)tamanoPagina);

            var elementos = await consulta
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToListAsync(cancellationToken);

            return new ResultadoPaginadoDto<Venta>
            {
                PaginaActual = pagina,
                TamanoPagina = tamanoPagina,
                TotalElementos = totalElementos,
                TotalPaginas = totalPaginas,
                Elementos = elementos
            };
        }

        public async Task<Venta?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _contexto.Ventas
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.VentaId == id, cancellationToken);
        }

        public async Task<ICollection<Venta>> ObtenerPorClienteAsync(
            int clienteId, 
            CancellationToken cancellationToken = default)
        {
            return await _contexto.Ventas
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(v => v.ClienteId == clienteId)
                .ToListAsync(cancellationToken);
        }

        public async Task<Venta> RegistrarVentaAsync(
            Venta venta, 
            CancellationToken cancellationToken = default)
        {
            if (venta.Detalles == null || !venta.Detalles.Any())
            {
                throw new InvalidOperationException("La venta debe contener al menos un producto en sus detalles.");
            }

            bool clienteExiste = await _contexto.Clientes
                .AnyAsync(c => c.ClienteId == venta.ClienteId, cancellationToken);

            if (!clienteExiste)
            {
                throw new InvalidOperationException($"El cliente con ID {venta.ClienteId} no existe.");
            }

            await using var transaccion = await _contexto.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (venta.Fecha == default)
                {
                    venta.Fecha = DateTime.UtcNow;
                }

                venta.Cliente = null!;

                foreach (var detalle in venta.Detalles)
                {
                    if (detalle.Cantidad <= 0)
                    {
                        throw new ArgumentException($"La cantidad para el producto ID {detalle.ProductoId} debe ser mayor a cero.");
                    }

                    if (detalle.PrecioUnitario <= 0)
                    {
                        throw new ArgumentException($"El precio unitario para el producto ID {detalle.ProductoId} debe ser mayor a cero.");
                    }

                    var producto = await _contexto.Productos
                        .FirstOrDefaultAsync(p => p.ProductoId == detalle.ProductoId, cancellationToken);

                    if (producto == null)
                    {
                        throw new KeyNotFoundException($"No se encontró el producto con ID {detalle.ProductoId}.");
                    }

                    // Validación crítica de inventario
                    if (producto.Stock < detalle.Cantidad)
                    {
                        throw new InvalidOperationException($"Stock insuficiente para el producto '{producto.Nombre}'. Stock disponible: {producto.Stock}, Solicitado: {detalle.Cantidad}.");
                    }

                    // Decremento de stock por venta
                    producto.Stock -= detalle.Cantidad;

                    detalle.Producto = null!;
                    detalle.Venta = null!;
                }

                _contexto.Ventas.Add(venta);
                await _contexto.SaveChangesAsync(cancellationToken);

                await transaccion.CommitAsync(cancellationToken);

                // Cargar datos relacionados para la respuesta
                await _contexto.Entry(venta)
                    .Reference(v => v.Cliente)
                    .LoadAsync(cancellationToken);

                await _contexto.Entry(venta)
                    .Collection(v => v.Detalles)
                    .Query()
                    .Include(d => d.Producto)
                    .LoadAsync(cancellationToken);

                return venta;
            }
            catch
            {
                await transaccion.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<bool> AnularVentaAsync(int id, CancellationToken cancellationToken = default)
        {
            await using var transaccion = await _contexto.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var venta = await _contexto.Ventas
                    .Include(v => v.Detalles)
                    .FirstOrDefaultAsync(v => v.VentaId == id, cancellationToken);

                if (venta == null)
                {
                    throw new KeyNotFoundException($"No se encontró la venta con ID {id}.");
                }

                // Reincorporar el stock de los productos vendidos
                foreach (var detalle in venta.Detalles)
                {
                    var producto = await _contexto.Productos
                        .FirstOrDefaultAsync(p => p.ProductoId == detalle.ProductoId, cancellationToken);

                    if (producto != null)
                    {
                        producto.Stock += detalle.Cantidad;
                    }
                }

                _contexto.DetallesVenta.RemoveRange(venta.Detalles);
                _contexto.Ventas.Remove(venta);

                await _contexto.SaveChangesAsync(cancellationToken);
                await transaccion.CommitAsync(cancellationToken);

                return true;
            }
            catch
            {
                await transaccion.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}