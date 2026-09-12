using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using PracticoN5ServicioApiRest.Models;
using PracticoN5ServicioApiRest.Services;
using PracticoN5ServicioApiRest.DTOs;

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

        [HttpGet("test")]
        [AllowAnonymous]
        public IActionResult Test()
        {
            return Ok("API funcionando");
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request, CancellationToken cancellationToken = default)
        {
            var token = await _servicio.LoginAsync(
                request.NombreUsuario,
                request.Password,
                cancellationToken);

            if (token == null)
            {
                return Unauthorized("Nombre de usuario o contraseña incorrectos.");
            }

            return Ok(new LoginResponseDTO
            {
                Token = token
            });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ObtenerTodos(CancellationToken cancellationToken = default)
        {
            var usuarios = await _servicio.ObtenerTodosAsync(cancellationToken);
            return Ok(usuarios);
        }

        [Authorize]
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

        [Authorize]
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

        [Authorize]
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

        [Authorize]
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

        [Authorize]
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