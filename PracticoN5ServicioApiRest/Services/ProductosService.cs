using Microsoft.EntityFrameworkCore;
using PracticoN5ServicioApiRest.Data;
using PracticoN5ServicioApiRest.DTOs;
using PracticoN5ServicioApiRest.Models;

namespace PracticoN5ServicioApiRest.Services
{
    public class ProductosService
    {
        private readonly SistemaVentasDbContext _contexto;

        public ProductosService(SistemaVentasDbContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<ResultadoPaginadoDto<Producto>> ObtenerTodosAsync(
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

            var consulta = _contexto.Productos
                .AsNoTracking()
                .Include(p => p.Categoria);

            int totalElementos = await consulta.CountAsync(cancellationToken);
            int totalPaginas = (int)Math.Ceiling(totalElementos / (double)tamanoPagina);

            var elementos = await consulta
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToListAsync(cancellationToken);

            return new ResultadoPaginadoDto<Producto>
            {
                PaginaActual = pagina,
                TamanoPagina = tamanoPagina,
                TotalElementos = totalElementos,
                TotalPaginas = totalPaginas,
                Elementos = elementos
            };
        }

        // Método de compatibilidad para consultas completas
        public async Task<ICollection<Producto>> GetProductosAsync(CancellationToken cancellationToken = default)
        {
            return await _contexto.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .ToListAsync(cancellationToken);
        }

        public async Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _contexto.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.ProductoId == id, cancellationToken);
        }

        public async Task<ICollection<Producto>> ObtenerPorCategoriaAsync(int categoriaId, CancellationToken cancellationToken = default)
        {
            return await _contexto.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Where(p => p.CategoriaId == categoriaId)
                .ToListAsync(cancellationToken);
        }

        public async Task<ResponseProductoDTO> CrearAsync(CreateProductoDTO productoDto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(productoDto.Nombre))
            {
                throw new ArgumentException("El nombre del producto es obligatorio.", nameof(productoDto));
            }

            if (productoDto.Precio < 0)
            {
                throw new ArgumentException("El precio del producto no puede ser negativo.", nameof(productoDto));
            }

            if (productoDto.Stock < 0)
            {
                throw new ArgumentException("El stock inicial no puede ser negativo.", nameof(productoDto));
            }

            bool categoriaExiste = await _contexto.Categorias
                .AnyAsync(c => c.CategoriaId == productoDto.CategoriaId, cancellationToken);

            if (!categoriaExiste)
            {
                throw new InvalidOperationException($"La categoría con ID {productoDto.CategoriaId} no existe.");
            }

            //Mapeo de DTO a Modelo
            var producto = new Producto
            {
                Nombre = productoDto.Nombre,
                Descripcion = productoDto.Descripcion,
                Precio = productoDto.Precio,
                Stock = productoDto.Stock,
                ImagenRuta = productoDto.ImagenRuta,
                CategoriaId = productoDto.CategoriaId
            };

            await _contexto.Productos.AddAsync(producto, cancellationToken);
            await _contexto.SaveChangesAsync(cancellationToken);

            // Cargar datos de la categoría para el retorno completo
            await _contexto.Entry(producto)
                .Reference(p => p.Categoria)
                .LoadAsync(cancellationToken);

            return new ResponseProductoDTO 
            {
                ProductoId = producto.ProductoId,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio, 
                Stock = producto.Stock,
                ImagenRuta = producto.ImagenRuta,
                CategoriaId = producto.CategoriaId
            };
        }

        public async Task<ResponseProductoDTO> ActualizarAsync(int id, CreateProductoDTO productoActualizado, CancellationToken cancellationToken = default)
        {
            var productoExistente = await _contexto.Productos
                .FirstOrDefaultAsync(p => p.ProductoId == id, cancellationToken);

            if (productoExistente == null)
            {
                throw new KeyNotFoundException($"No se encontró el producto con ID {id}.");
            }

            if (string.IsNullOrWhiteSpace(productoActualizado.Nombre))
            {
                throw new ArgumentException("El nombre del producto no puede estar vacío.", nameof(productoActualizado));
            }

            if (productoActualizado.Precio < 0)
            {
                throw new ArgumentException("El precio del producto no puede ser negativo.", nameof(productoActualizado));
            }

            if (productoActualizado.Stock < 0)
            {
                throw new ArgumentException("El stock del producto no puede ser negativo.", nameof(productoActualizado));
            }

            if (productoExistente.CategoriaId != productoActualizado.CategoriaId)
            {
                bool categoriaExiste = await _contexto.Categorias
                    .AnyAsync(c => c.CategoriaId == productoActualizado.CategoriaId, cancellationToken);

                if (!categoriaExiste)
                {
                    throw new InvalidOperationException($"La categoría con ID {productoActualizado.CategoriaId} no existe.");
                }
            }

            productoExistente.Nombre = productoActualizado.Nombre;
            productoExistente.Descripcion = productoActualizado.Descripcion;
            productoExistente.Precio = productoActualizado.Precio;
            productoExistente.Stock = productoActualizado.Stock;
            productoExistente.ImagenRuta = productoActualizado.ImagenRuta;
            productoExistente.CategoriaId = productoActualizado.CategoriaId;

            await _contexto.SaveChangesAsync(cancellationToken);

            await _contexto.Entry(productoExistente)
                .Reference(p => p.Categoria)
                .LoadAsync(cancellationToken);

            return new ResponseProductoDTO 
            {
                ProductoId = productoExistente.ProductoId,
                Nombre = productoExistente.Nombre,
                Descripcion = productoExistente.Descripcion,
                Precio = productoExistente.Precio,
                Stock = productoExistente.Stock,
                ImagenRuta = productoExistente.ImagenRuta,
                CategoriaId = productoExistente.CategoriaId
            };
        }

        public async Task<bool> ActualizarStockAsync(
            int productoId, 
            int cantidadCambio, 
            CancellationToken cancellationToken = default)
        {
            var producto = await _contexto.Productos
                .FirstOrDefaultAsync(p => p.ProductoId == productoId, cancellationToken);

            if (producto == null)
            {
                throw new KeyNotFoundException($"No se encontró el producto con ID {productoId}.");
            }

            int nuevoStock = producto.Stock + cantidadCambio;
            if (nuevoStock < 0)
            {
                throw new InvalidOperationException($"Stock insuficiente para el producto '{producto.Nombre}'. Stock actual: {producto.Stock}, Ajuste solicitado: {cantidadCambio}.");
            }

            producto.Stock = nuevoStock;
            await _contexto.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default)
        {
            var producto = await _contexto.Productos
                .FirstOrDefaultAsync(p => p.ProductoId == id, cancellationToken);

            if (producto == null)
            {
                throw new KeyNotFoundException($"No se encontró el producto con ID {id}.");
            }

            bool tieneCompras = await _contexto.DetallesCompra
                .AnyAsync(d => d.ProductoId == id, cancellationToken);

            if (tieneCompras)
            {
                throw new InvalidOperationException("No se puede eliminar el producto porque tiene compras registradas asociadas.");
            }

            bool tieneVentas = await _contexto.DetallesVenta
                .AnyAsync(d => d.ProductoId == id, cancellationToken);

            if (tieneVentas)
            {
                throw new InvalidOperationException("No se puede eliminar el producto porque tiene ventas registradas asociadas.");
            }

            _contexto.Productos.Remove(producto);
            await _contexto.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
