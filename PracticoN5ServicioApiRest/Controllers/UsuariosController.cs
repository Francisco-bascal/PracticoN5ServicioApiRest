using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PracticoN5ServicioApiRest.DTOs;
using PracticoN5ServicioApiRest.Models;
using PracticoN5ServicioApiRest.Services;
using System.Security.Claims;

namespace PracticoN5ServicioApiRest.Controllers
{
    // "test", "login" y "registro" son públicas ([AllowAnonymous]); el resto exige token JWT válido
    // (Crear, Actualizar y Eliminar solo para Administrador).
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
        /// <summary>Autentica credenciales y devuelve el token JWT a usar como Bearer en los [Authorize].</summary>
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request, CancellationToken cancellationToken = default)
        {
            var token = await _servicio.LoginAsync(
                request.NombreUsuario,
                request.Password,
                cancellationToken);

            // null = credenciales inválidas (usuario inexistente o contraseña incorrecta) -> 401.
            if (token == null)
            {
                return Unauthorized("Nombre de usuario o contraseña incorrectos.");
            }

            return Ok(new LoginResponseDTO
            {
                Token = token
            });
        }

        [AllowAnonymous]
        [HttpPost("registro")]
        /// <summary>Registra un usuario nuevo y le asigna automáticamente el rol Operador.</summary>
        public async Task<IActionResult> Registrar(
            [FromBody] RegistrarUsuarioDTO request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var usuarioCreado = await _servicio.RegistrarOperadorAsync(request, cancellationToken);
                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { id = usuarioCreado.UsuarioId },
                    MapearARespuesta(usuarioCreado));
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
        [HttpGet]
        public async Task<IActionResult> ObtenerTodos(CancellationToken cancellationToken = default)
        {
            var usuarios = await _servicio.ObtenerTodosAsync(cancellationToken);
            return Ok(usuarios.Select(MapearARespuesta).ToList());
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

            return Ok(MapearARespuesta(usuario));
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

            return Ok(MapearARespuesta(usuario));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        public async Task<IActionResult> Crear(
            [FromBody] CrearUsuarioDTO request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var usuarioCreado = await _servicio.CrearAsync(request, cancellationToken);
                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { id = usuarioCreado.UsuarioId },
                    MapearARespuesta(usuarioCreado));
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

        [Authorize(Roles = "Administrador")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(
            int id,
            [FromBody] ActualizarUsuarioDTO request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var usuarioActualizado = await _servicio.ActualizarAsync(
                    id,
                    request,
                    ObtenerIdUsuarioActual(),
                    cancellationToken);
                return Ok(MapearARespuesta(usuarioActualizado));
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

        [Authorize(Roles = "Administrador")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _servicio.EliminarAsync(id, ObtenerIdUsuarioActual(), cancellationToken);
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

        private int ObtenerIdUsuarioActual()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        //Conversión de la entidad de usuario a un DTO que solo expone información no sensible
        private static UsuarioResponseDTO MapearARespuesta(Usuario usuario)
        {
            return new UsuarioResponseDTO
            {
                UsuarioId = usuario.UsuarioId,
                NombreUsuario = usuario.NombreUsuario,
                Email = usuario.Email,
                Rol = usuario.Rol
            };
        }
    }
}