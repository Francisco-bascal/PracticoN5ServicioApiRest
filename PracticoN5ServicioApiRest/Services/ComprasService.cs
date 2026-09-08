using Microsoft.EntityFrameworkCore;
using PracticoN5ServicioApiRest.Data;
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

        public async Task<ICollection<Compra>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        {
            return await _contexto.Compras
                .AsNoTracking()
                .Include(c => c.Proveedor)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Producto)
                .ToListAsync(cancellationToken);
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

        public async Task<Compra> RegistrarCompraAsync(
            Compra compra, 
            CancellationToken cancellationToken = default)
        {
            if (compra.Detalles == null || !compra.Detalles.Any())
            {
                throw new InvalidOperationException("La compra debe incluir al menos una línea de detalle con un producto.");
            }

            bool proveedorExiste = await _contexto.Proveedores
                .AnyAsync(p => p.ProveedorId == compra.ProveedorId, cancellationToken);

            if (!proveedorExiste)
            {
                throw new InvalidOperationException($"El proveedor con ID {compra.ProveedorId} no existe.");
            }

            await using var transaccion = await _contexto.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (compra.Fecha == default)
                {
                    compra.Fecha = DateTime.UtcNow;
                }

                compra.Proveedor = null!;

                foreach (var detalle in compra.Detalles)
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
                        throw new KeyNotFoundException($"No se encontró el producto con ID {detalle.ProductoId} para asociar al detalle de compra.");
                    }

                    // Aumentar stock del producto con la compra
                    producto.Stock += detalle.Cantidad;

                    detalle.Producto = null!;
                    detalle.Compra = null!;
                }

                _contexto.Compras.Add(compra);
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