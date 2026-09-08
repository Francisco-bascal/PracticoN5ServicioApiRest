using Microsoft.AspNetCore.Mvc;
using PracticoN5ServicioApiRest.Models;
using PracticoN5ServicioApiRest.Services;

namespace PracticoN5ServicioApiRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly UsuariosService _servicio;

        public UsuariosController(UsuariosService servicio)
        {
            _servicio = servicio;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos(CancellationToken cancellationToken = default)
        {
            var usuarios = await _servicio.ObtenerTodosAsync(cancellationToken);
            return Ok(usuarios);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id, CancellationToken cancellationToken = default)
        {
            var usuario = await _servicio.ObtenerPorIdAsync(id, cancellationToken);
            if (usuario == null)
            {
                return NotFound($"No se encontró el usuario con ID {id}.");
            }

            return Ok(usuario);
        }

        [HttpGet("usuario/{nombreUsuario}")]
        public async Task<IActionResult> ObtenerPorNombreUsuario(
            string nombreUsuario, 
            CancellationToken cancellationToken = default)
        {
            var usuario = await _servicio.ObtenerPorNombreUsuarioAsync(nombreUsuario, cancellationToken);
            if (usuario == null)
            {
                return NotFound($"No se encontró el usuario con nombre '{nombreUsuario}'.");
            }

            return Ok(usuario);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(
            [FromBody] Usuario usuario, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var usuarioCreado = await _servicio.CrearAsync(usuario, cancellationToken);
                return CreatedAtAction(
                    nameof(ObtenerPorId), 
                    new { id = usuarioCreado.UsuarioId }, 
                    usuarioCreado);
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
            [FromBody] Usuario usuario, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var usuarioActualizado = await _servicio.ActualizarAsync(id, usuario, cancellationToken);
                return Ok(usuarioActualizado);
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
        }
    }
}
