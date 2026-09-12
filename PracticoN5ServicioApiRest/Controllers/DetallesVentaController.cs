using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PracticoN5ServicioApiRest.Services;

namespace PracticoN5ServicioApiRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DetallesVentaController : ControllerBase
    {
        private readonly DetallesVentaService _servicio;

        public DetallesVentaController(DetallesVentaService servicio)
        {
            _servicio = servicio;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id, CancellationToken cancellationToken = default)
        {
            var detalle = await _servicio.ObtenerPorIdAsync(id, cancellationToken);
            if (detalle == null)
            {
                return NotFound($"No se encontró el detalle de venta con ID {id}.");
            }

            return Ok(detalle);
        }

        [HttpGet("venta/{ventaId:int}")]
        public async Task<IActionResult> ObtenerPorVenta(int ventaId, CancellationToken cancellationToken = default)
        {
            var detalles = await _servicio.ObtenerPorVentaIdAsync(ventaId, cancellationToken);
            return Ok(detalles);
        }

        [HttpGet("producto/{productoId:int}")]
        public async Task<IActionResult> ObtenerPorProducto(int productoId, CancellationToken cancellationToken = default)
        {
            var detalles = await _servicio.ObtenerPorProductoIdAsync(productoId, cancellationToken);
            return Ok(detalles);
        }
    }
}
