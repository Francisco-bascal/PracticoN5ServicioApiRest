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

        /// <summary>Devuelve las ventas paginadas.</summary>
        /// <param name="pagina">Número de página (default 1).</param>
        /// <param name="tamanoPagina">Elementos por página (default 10, máx. 100).</param>
        public async Task<ResultadoPaginadoDto<Venta>> ObtenerTodosAsync(
            int pagina = 1, 
            int tamanoPagina = 10, 
            CancellationToken cancellationToken = default)
        {
            // Validación de los parámetros de paginación.
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

            // AsNoTracking: consulta de solo lectura. Include: carga cliente y detalles con producto.
            var consulta = _contexto.Ventas
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto);

            // Total de elementos (para calcular el total de páginas).
            int totalElementos = await consulta.CountAsync(cancellationToken);

            // Total de páginas, redondeando hacia arriba.
            int totalPaginas = (int)Math.Ceiling(totalElementos / (double)tamanoPagina);

            // Skip: salta las páginas anteriores. Take: toma la página actual.
            // ToListAsync: materializa la consulta.
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

        /// <summary>Registra una venta, valida stock suficiente y lo decrementa, todo en una transacción.</summary>
        /// <param name="ventaDto">Datos de la venta: cliente, fecha opcional y líneas de detalle.</param>
        public async Task<Venta> RegistrarVentaAsync(CreateVentaDTO ventaDto, CancellationToken cancellationToken = default)
        {
            // Debe tener al menos una línea de detalle.
            if (ventaDto.Detalles == null || !ventaDto.Detalles.Any())
            {
                throw new InvalidOperationException("La venta debe contener al menos un producto en sus detalles.");
            }

            // El cliente debe existir.
            bool clienteExiste = await _contexto.Clientes
                .AnyAsync(c => c.ClienteId == ventaDto.ClienteId, cancellationToken);

            if (!clienteExiste)
            {
                throw new InvalidOperationException($"El cliente con ID {ventaDto.ClienteId} no existe.");
            }

            // Inicia una transacción: venta, detalles y stock se confirman o revierten juntos.
            await using var transaccion =
                await _contexto.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var venta = new Venta
                {
                    // Fecha por defecto: la actual en UTC.
                    Fecha = ventaDto.Fecha == default
                        ? DateTime.UtcNow
                        : ventaDto.Fecha,

                    ClienteId = ventaDto.ClienteId
                };

                foreach (var detalleDto in ventaDto.Detalles)
                {
                    // Validación de cada línea: cantidad y precio deben ser positivos.
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
                        .FirstOrDefaultAsync(p => p.ProductoId == detalleDto.ProductoId, cancellationToken);

                    if (producto == null)
                    {
                        throw new KeyNotFoundException($"No se encontró el producto con ID {detalleDto.ProductoId}.");
                    }

                    // No se permite vender sin stock suficiente.
                    if (producto.Stock < detalleDto.Cantidad)
                    {
                        throw new InvalidOperationException(
                            $"Stock insuficiente para el producto '{producto.Nombre}'. " +
                            $"Stock disponible: {producto.Stock}, " +
                            $"Solicitado: {detalleDto.Cantidad}.");
                    }

                    //Lógica de ajuste de cantidad en stock: vender resta stock.
                    producto.Stock -= detalleDto.Cantidad;

                    var detalle = new DetalleVenta
                    {
                        ProductoId = detalleDto.ProductoId,
                        Cantidad = detalleDto.Cantidad,
                        PrecioUnitario = detalleDto.PrecioUnitario
                    };

                    venta.Detalles.Add(detalle);
                }

                // Guarda los cambios dentro de la transacción.
                await _contexto.Ventas.AddAsync(venta, cancellationToken);
                await _contexto.SaveChangesAsync(cancellationToken);

                // Confirma los cambios definitivamente.
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
                // Ante cualquier error deshace todo y relanza la excepción.
                await transaccion.RollbackAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>Anula una venta y devuelve el stock que había descontado, todo en una transacción.</summary>
        /// <param name="id">Identificador de la venta a anular.</param>
        public async Task<bool> AnularVentaAsync(int id, CancellationToken cancellationToken = default)
        {
            // Transacción: stock, detalles y cabecera se modifican juntos.
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
                        // Suma stock: no requiere validación de disponibilidad (a diferencia de anular compra).
                        producto.Stock += detalle.Cantidad;
                    }
                }

                //Primero se eliminan los detalles para luego poder eliminar la venta.
                //El orden es obligatorio por la restricción DeleteBehavior.Restrict.
                _contexto.DetallesVenta.RemoveRange(venta.Detalles);
                _contexto.Ventas.Remove(venta);

                // Guarda y confirma los cambios.
                await _contexto.SaveChangesAsync(cancellationToken);
                await transaccion.CommitAsync(cancellationToken);

                return true;
            }
            catch
            {
                // Deshace todo (stock y eliminaciones) y relanza la excepción.
                await transaccion.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}