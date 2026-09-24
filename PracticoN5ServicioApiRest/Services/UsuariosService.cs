using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PracticoN5ServicioApiRest.Data;
using PracticoN5ServicioApiRest.DTOs;
using PracticoN5ServicioApiRest.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PracticoN5ServicioApiRest.Services
{
    public class UsuariosService
    {
        private readonly SistemaVentasDbContext _contexto;
        private readonly IConfiguration _configuracion;
        private readonly PasswordHasher<Usuario> _passwordHasher;

        public UsuariosService(SistemaVentasDbContext contexto, IConfiguration configuracion)
        {
            _contexto = contexto;
            _configuracion = configuracion;
            _passwordHasher = new PasswordHasher<Usuario>();
        }

        /// <summary>Valida las credenciales y, si son correctas, genera el token JWT firmado con la expiración de la configuración.</summary>
        /// <param name="nombreUsuario">Nombre de usuario.</param>
        /// <param name="password">Contraseña en texto plano.</param>
        /// <returns>El token JWT serializado, o null si las credenciales son inválidas.</returns>
        public async Task<string?> LoginAsync(string nombreUsuario, string password, CancellationToken cancellationToken = default)
        {
            // Busca el usuario por nombre, insensible a mayúsculas/minúsculas.
            var usuario = await _contexto.Usuarios
                .FirstOrDefaultAsync(
                    u => u.NombreUsuario.ToLower() == nombreUsuario.ToLower(),
                    cancellationToken);

            if (usuario == null)
            {
                return null;
            }

            // Compara la contraseña ingresada con el hash guardado en la base de datos.
            var resultadoContraseña = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password);

            if (resultadoContraseña == PasswordVerificationResult.Failed)
            {
                return null;
            }

            // Claims del usuario que viajan dentro del token.
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            // Clave simétrica de firma, la misma que usa Program.cs para validar.
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _configuracion["Jwt:Key"]!));

            // Algoritmo de firma HMAC-SHA256.
            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            // Emisor, claims, expiración (de la configuración) y firma.
            var token = new JwtSecurityToken(
                issuer: _configuracion["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_configuracion.GetValue<int?>("Jwt:ExpirationMinutes") ?? 60),
                signingCredentials: credentials);

            // Serializa el token a su forma de string (header.payload.signature).
            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }

        public async Task<ICollection<Usuario>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        {
            return await _contexto.Usuarios
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _contexto.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UsuarioId == id, cancellationToken);
        }

        public async Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default)
        {
            return await _contexto.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.NombreUsuario.ToLower() == nombreUsuario.ToLower(), cancellationToken);
        }

        public async Task<Usuario> RegistrarOperadorAsync(RegistrarUsuarioDTO dto, CancellationToken cancellationToken = default)
        {
            await ValidarUnicidadAsync(dto.NombreUsuario, dto.Email, null, cancellationToken);

            // El registro público asigna siempre el rol Operador.
            var usuario = new Usuario
            {
                NombreUsuario = dto.NombreUsuario,
                Email = dto.Email,
                Rol = "Operador",
                PasswordHash = _passwordHasher.HashPassword(new Usuario(), dto.Password)
            };

            await _contexto.Usuarios.AddAsync(usuario, cancellationToken);
            await _contexto.SaveChangesAsync(cancellationToken);

            return usuario;
        }

        public async Task<Usuario> CrearAsync(CrearUsuarioDTO dto, CancellationToken cancellationToken = default)
        {
            ValidarRol(dto.Rol);

            await ValidarUnicidadAsync(dto.NombreUsuario, dto.Email, null, cancellationToken);

            var usuario = new Usuario
            {
                NombreUsuario = dto.NombreUsuario,
                Email = dto.Email,
                Rol = dto.Rol,
                PasswordHash = _passwordHasher.HashPassword(new Usuario(), dto.Password)
            };

            await _contexto.Usuarios.AddAsync(usuario, cancellationToken);
            await _contexto.SaveChangesAsync(cancellationToken);

            return usuario;
        }

        public async Task<Usuario> ActualizarAsync(
            int id,
            ActualizarUsuarioDTO dto,
            int idUsuarioActual,
            CancellationToken cancellationToken = default)
        {
            ValidarRol(dto.Rol);

            var usuarioExistente = await _contexto.Usuarios
                .FirstOrDefaultAsync(u => u.UsuarioId == id, cancellationToken);

            if (usuarioExistente == null)
            {
                throw new KeyNotFoundException($"No se encontró el usuario con ID {id}.");
            }

            await ValidarUnicidadAsync(dto.NombreUsuario, dto.Email, id, cancellationToken);

            // No se puede degradar al único administrador del sistema.
            if (usuarioExistente.Rol == "Administrador"
                && dto.Rol != "Administrador"
                && await EsElUnicoAdministradorAsync(id, cancellationToken))
            {
                throw new InvalidOperationException("No se puede degradar al único administrador del sistema.");
            }

            usuarioExistente.NombreUsuario = dto.NombreUsuario;
            usuarioExistente.Email = dto.Email;
            usuarioExistente.Rol = dto.Rol;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                usuarioExistente.PasswordHash = _passwordHasher.HashPassword(usuarioExistente, dto.Password);
            }

            await _contexto.SaveChangesAsync(cancellationToken);

            return usuarioExistente;
        }

        public async Task<bool> EliminarAsync(int id, int idUsuarioActual, CancellationToken cancellationToken = default)
        {
            if (id == idUsuarioActual)
            {
                throw new InvalidOperationException("No se puede eliminar el propio usuario.");
            }

            var usuario = await _contexto.Usuarios
                .FirstOrDefaultAsync(u => u.UsuarioId == id, cancellationToken);

            if (usuario == null)
            {
                throw new KeyNotFoundException($"No se encontró el usuario con ID {id}.");
            }

            // No se puede eliminar al único administrador del sistema.
            if (await EsElUnicoAdministradorAsync(id, cancellationToken))
            {
                throw new InvalidOperationException("No se puede eliminar al único administrador del sistema.");
            }

            _contexto.Usuarios.Remove(usuario);
            await _contexto.SaveChangesAsync(cancellationToken);

            return true;
        }

        private static void ValidarRol(string rol)
        {
            if (rol != "Administrador" && rol != "Operador")
            {
                throw new ArgumentException("El rol debe ser 'Administrador' o 'Operador'.", nameof(rol));
            }
        }

        private async Task ValidarUnicidadAsync(
            string? nombreUsuario,
            string? email,
            int? idExcepto,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario))
            {
                throw new ArgumentException("El nombre de usuario es obligatorio.", nameof(nombreUsuario));
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("El correo electrónico es obligatorio.", nameof(email));
            }

            IQueryable<Usuario> consulta = _contexto.Usuarios;

            if (idExcepto.HasValue)
            {
                consulta = consulta.Where(u => u.UsuarioId != idExcepto.Value);
            }

            if (await consulta.AnyAsync(u => u.NombreUsuario.ToLower() == nombreUsuario.ToLower(), cancellationToken))
            {
                throw new InvalidOperationException($"El nombre de usuario '{nombreUsuario}' ya está en uso.");
            }

            if (await consulta.AnyAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken))
            {
                throw new InvalidOperationException($"El correo electrónico '{email}' ya está registrado.");
            }
        }

        private async Task<bool> EsElUnicoAdministradorAsync(int idUsuario, CancellationToken cancellationToken)
        {
            bool esAdministrador = await _contexto.Usuarios
                .AnyAsync(u => u.UsuarioId == idUsuario && u.Rol == "Administrador", cancellationToken);

            if (!esAdministrador)
            {
                return false;
            }

            return await _contexto.Usuarios
                .CountAsync(u => u.Rol == "Administrador", cancellationToken) == 1;
        }
    }
}