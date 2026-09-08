using Microsoft.AspNetCore.Mvc;
using PracticoN5ServicioApiRest.Models;
using PracticoN5ServicioApiRest.Services;

namespace PracticoN5ServicioApiRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VentasController : ControllerBase
    {
        private readonly VentasService _servicio;

        public VentasController(VentasService servicio)
        {
            _servicio = servicio;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos(
            [FromQuery] int pagina = 1, 
            [FromQuery] int tamanoPagina = 10, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var ventas = await _servicio.ObtenerTodosAsync(pagina, tamanoPagina, cancellationToken);
                return Ok(ventas);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id, CancellationToken cancellationToken = default)
        {
            var venta = await _servicio.ObtenerPorIdAsync(id, cancellationToken);
            if (venta == null)
            {
                return NotFound($"No se encontró la venta con ID {id}.");
            }

            return Ok(venta);
        }

        [HttpGet("cliente/{clienteId:int}")]
        public async Task<IActionResult> ObtenerPorCliente(
            int clienteId, 
            CancellationToken cancellationToken = default)
        {
            var ventas = await _servicio.ObtenerPorClienteAsync(clienteId, cancellationToken);
            return Ok(ventas);
        }

        [HttpPost]
        public async Task<IActionResult> Registrar(
            [FromBody] Venta venta, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var ventaRegistrada = await _servicio.RegistrarVentaAsync(venta, cancellationToken);
                return CreatedAtAction(
                    nameof(ObtenerPorId), 
                    new { id = ventaRegistrada.VentaId }, 
                    ventaRegistrada);
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
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id:int}/anular")]
        public async Task<IActionResult> Anular(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _servicio.AnularVentaAsync(id, cancellationToken);
                return Ok("Venta anulada exitosamente y stock restituido.");
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
