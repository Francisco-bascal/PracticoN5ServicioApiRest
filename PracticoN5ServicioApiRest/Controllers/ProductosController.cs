using Microsoft.AspNetCore.Mvc;
using PracticoN5ServicioApiRest.Models;
using PracticoN5ServicioApiRest.Services;

namespace PracticoN5ServicioApiRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly ProductosService _servicio;

        public ProductosController(ProductosService servicio)
        {
            _servicio = servicio;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            [FromQuery] int pagina = 1, 
            [FromQuery] int tamanoPagina = 10, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var productos = await _servicio.ObtenerTodosAsync(pagina, tamanoPagina, cancellationToken);
                return Ok(productos);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id, CancellationToken cancellationToken = default)
        {
            var producto = await _servicio.ObtenerPorIdAsync(id, cancellationToken);
            if (producto == null)
            {
                return NotFound($"No se encontró el producto con ID {id}.");
            }

            return Ok(producto);
        }

        [HttpGet("categoria/{categoriaId:int}")]
        public async Task<IActionResult> ObtenerPorCategoria(int categoriaId, CancellationToken cancellationToken = default)
        {
            var productos = await _servicio.ObtenerPorCategoriaAsync(categoriaId, cancellationToken);
            return Ok(productos);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(
            [FromBody] Producto producto, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var productoCreado = await _servicio.CrearAsync(producto, cancellationToken);
                return CreatedAtAction(
                    nameof(ObtenerPorId), 
                    new { id = productoCreado.ProductoId }, 
                    productoCreado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(
            int id, 
            [FromBody] Producto producto, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var productoActualizado = await _servicio.ActualizarAsync(id, producto, cancellationToken);
                return Ok(productoActualizado);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPatch("{id:int}/stock")]
        public async Task<IActionResult> ActualizarStock(
            int id, 
            [FromBody] int cantidadCambio, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _servicio.ActualizarStockAsync(id, cantidadCambio, cancellationToken);
                return Ok("Stock actualizado correctamente.");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _servicio.EliminarAsync(id, cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }
    }
}