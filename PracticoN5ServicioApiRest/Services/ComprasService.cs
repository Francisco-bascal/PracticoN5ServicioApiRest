using Microsoft.EntityFrameworkCore;
using PracticoN5ServicioApiRest.Data;
using PracticoN5ServicioApiRest.DTOs;
using PracticoN5ServicioApiRest.Models;

namespace PracticoN5ServicioApiRest.Services
{
    public class ComprasService
    {
        private readonly SistemaVentasDbContext _contexto;

        public ComprasService(SistemaVentasDbContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<ResultadoPaginadoDto<Compra>> ObtenerTodosAsync(
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

            var consulta = _contexto.Compras
                .AsNoTracking()
                .Include(c => c.Proveedor)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Producto);

            int totalElementos = await consulta.CountAsync(cancellationToken);
            int totalPaginas = (int)Math.Ceiling(totalElementos / (double)tamanoPagina);

            var elementos = await consulta
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToListAsync(cancellationToken);

            return new ResultadoPaginadoDto<Compra>
            {
                PaginaActual = pagina,
                TamanoPagina = tamanoPagina,
                TotalElementos = totalElementos,
                TotalPaginas = totalPaginas,
                Elementos = elementos
            };
        }

        public async Task<Compra?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _contexto.Compras
                .AsNoTracking()
                .Include(c => c.Proveedor)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(c => c.CompraId == id, cancellationToken);
        }

        public async Task<ICollection<Compra>> ObtenerPorProveedorAsync(
            int proveedorId, 
            CancellationToken cancellationToken = default)
        {
            return await _contexto.Compras
                .AsNoTracking()
                .Include(c => c.Proveedor)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(c => c.ProveedorId == proveedorId)
                .ToListAsync(cancellationToken);
        }

        public async Task<Compra> RegistrarCompraAsync(CreateCompraDTO compraDto, CancellationToken cancellationToken = default)
        {
            if (compraDto.Detalles == null || !compraDto.Detalles.Any())
            {
                throw new InvalidOperationException(
                    "La compra debe incluir al menos una línea de detalle con un producto.");
            }

            bool proveedorExiste = await _contexto.Proveedores
                .AnyAsync(
                    p => p.ProveedorId == compraDto.ProveedorId,
                    cancellationToken);

            if (!proveedorExiste)
            {
                throw new InvalidOperationException(
                    $"El proveedor con ID {compraDto.ProveedorId} no existe.");
            }

            await using var transaccion =
                await _contexto.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var compra = new Compra
                {
                    Fecha = compraDto.Fecha == default
                        ? DateTime.UtcNow
                        : compraDto.Fecha,

                    ProveedorId = compraDto.ProveedorId
                };

                foreach (var detalleDto in compraDto.Detalles)
                {
                    if (detalleDto.Cantidad <= 0)
                    {
                        throw new ArgumentException(
                            $"La cantidad para el producto ID {detalleDto.ProductoId} debe ser mayor a cero.");
                    }

                    if (detalleDto.PrecioUnitario <= 0)
                    {
                        throw new ArgumentException(
                            $"El precio unitario para el producto ID {detalleDto.ProductoId} debe ser mayor a cero.");
                    }

                    var producto = await _contexto.Productos
                        .FirstOrDefaultAsync(
                            p => p.ProductoId == detalleDto.ProductoId,
                            cancellationToken);

                    if (producto == null)
                    {
                        throw new KeyNotFoundException(
                            $"No se encontró el producto con ID {detalleDto.ProductoId} para asociar al detalle de compra.");
                    }

                    //Lógica de adición de stock
                    producto.Stock += detalleDto.Cantidad;

                    var detalle = new DetalleCompra
                    {
                        ProductoId = detalleDto.ProductoId,
                        Cantidad = detalleDto.Cantidad,
                        PrecioUnitario = detalleDto.PrecioUnitario
                    };

                    compra.Detalles.Add(detalle);
                }

                await _contexto.Compras.AddAsync(compra, cancellationToken);
                await _contexto.SaveChangesAsync(cancellationToken);
                await transaccion.CommitAsync(cancellationToken);

                // Cargar datos relacionados para la respuesta
                await _contexto.Entry(compra)
                    .Reference(c => c.Proveedor)
                    .LoadAsync(cancellationToken);

                await _contexto.Entry(compra)
                    .Collection(c => c.Detalles)
                    .Query()
                    .Include(d => d.Producto)
                    .LoadAsync(cancellationToken);

                return compra;
            }
            catch
            {
                await transaccion.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<bool> AnularCompraAsync(int id, CancellationToken cancellationToken = default)
        {
            await using var transaccion = await _contexto.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var compra = await _contexto.Compras
                    .Include(c => c.Detalles)
                    .FirstOrDefaultAsync(c => c.CompraId == id, cancellationToken);

                if (compra == null)
                {
                    throw new KeyNotFoundException($"No se encontró la compra con ID {id}.");
                }

                // Revertir el stock agregado en la compra
                foreach (var detalle in compra.Detalles)
                {
                    var producto = await _contexto.Productos
                        .FirstOrDefaultAsync(p => p.ProductoId == detalle.ProductoId, cancellationToken);

                    if (producto != null)
                    {
                        if (producto.Stock < detalle.Cantidad)
                        {
                            throw new InvalidOperationException($"No es posible anular la compra: el stock actual de '{producto.Nombre}' ({producto.Stock}) es inferior a la cantidad comprada ({detalle.Cantidad}) a revertir.");
                        }

                        producto.Stock -= detalle.Cantidad;
                    }
                }

                _contexto.DetallesCompra.RemoveRange(compra.Detalles);
                _contexto.Compras.Remove(compra);

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