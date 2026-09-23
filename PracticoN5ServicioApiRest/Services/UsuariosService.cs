using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PracticoN5ServicioApiRest.Data;
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

        /// <summary>Valida las credenciales y, si son correctas, genera el token JWT firmado y expirado a 1 hora.</summary>
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

            // Emisor, claims, expiración (1 hora) y firma.
            var token = new JwtSecurityToken(
                issuer: _configuracion["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
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

        public async Task<Usuario> CrearAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(usuario.NombreUsuario))
            {
                throw new ArgumentException("El nombre de usuario es obligatorio.", nameof(usuario));
            }

            if (string.IsNullOrWhiteSpace(usuario.Email))
            {
                throw new ArgumentException("El correo electrónico es obligatorio.", nameof(usuario));
            }

            bool nombreUsuarioExiste = await _contexto.Usuarios
                .AnyAsync(u => u.NombreUsuario.ToLower() == usuario.NombreUsuario.ToLower(), cancellationToken);

            if (nombreUsuarioExiste)
            {
                throw new InvalidOperationException($"El nombre de usuario '{usuario.NombreUsuario}' ya está en uso.");
            }

            bool emailExiste = await _contexto.Usuarios
                .AnyAsync(u => u.Email.ToLower() == usuario.Email.ToLower(), cancellationToken);

            if (emailExiste)
            {
                throw new InvalidOperationException($"El correo electrónico '{usuario.Email}' ya está registrado.");
            }

            usuario.PasswordHash = _passwordHasher.HashPassword(usuario, usuario.PasswordHash);

            await _contexto.Usuarios.AddAsync(usuario, cancellationToken);
            await _contexto.SaveChangesAsync(cancellationToken);

            return usuario;
        }

        public async Task<Usuario> ActualizarAsync(
            int id, 
            Usuario usuarioActualizado, 
            CancellationToken cancellationToken = default)
        {
            var usuarioExistente = await _contexto.Usuarios
                .FirstOrDefaultAsync(u => u.UsuarioId == id, cancellationToken);

            if (usuarioExistente == null)
            {
                throw new KeyNotFoundException($"No se encontró el usuario con ID {id}.");
            }

            if (string.IsNullOrWhiteSpace(usuarioActualizado.NombreUsuario))
            {
                throw new ArgumentException("El nombre de usuario no puede estar vacío.", nameof(usuarioActualizado));
            }

            if (string.IsNullOrWhiteSpace(usuarioActualizado.Email))
            {
                throw new ArgumentException("El correo electrónico no puede estar vacío.", nameof(usuarioActualizado));
            }

            bool nombreUsuarioExiste = await _contexto.Usuarios
                .AnyAsync(u => u.UsuarioId != id && u.NombreUsuario.ToLower() == usuarioActualizado.NombreUsuario.ToLower(), cancellationToken);

            if (nombreUsuarioExiste)
            {
                throw new InvalidOperationException($"El nombre de usuario '{usuarioActualizado.NombreUsuario}' ya está en uso.");
            }

            bool emailExiste = await _contexto.Usuarios
                .AnyAsync(u => u.UsuarioId != id && u.Email.ToLower() == usuarioActualizado.Email.ToLower(), cancellationToken);

            if (emailExiste)
            {
                throw new InvalidOperationException($"El correo electrónico '{usuarioActualizado.Email}' ya está registrado.");
            }

            usuarioExistente.NombreUsuario = usuarioActualizado.NombreUsuario;
            usuarioExistente.Email = usuarioActualizado.Email;
            usuarioExistente.Rol = usuarioActualizado.Rol;

            if (!string.IsNullOrWhiteSpace(usuarioActualizado.PasswordHash))
            {
                usuarioExistente.PasswordHash = _passwordHasher.HashPassword(
                    usuarioExistente,
                    usuarioActualizado.PasswordHash);
            }

            await _contexto.SaveChangesAsync(cancellationToken);

            return usuarioExistente;
        }

        public async Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default)
        {
            var usuario = await _contexto.Usuarios
                .FirstOrDefaultAsync(u => u.UsuarioId == id, cancellationToken);

            if (usuario == null)
            {
                throw new KeyNotFoundException($"No se encontró el usuario con ID {id}.");
            }

            _contexto.Usuarios.Remove(usuario);
            await _contexto.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}