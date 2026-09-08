using Microsoft.EntityFrameworkCore;
using PracticoN5ServicioApiRest.Data;
using PracticoN5ServicioApiRest.Models;

namespace PracticoN5ServicioApiRest.Services
{
    public class UsuariosService
    {
        private readonly SistemaVentasDbContext _contexto;

        public UsuariosService(SistemaVentasDbContext contexto)
        {
            _contexto = contexto;
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

            _contexto.Usuarios.Add(usuario);
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
                usuarioExistente.PasswordHash = usuarioActualizado.PasswordHash;
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
