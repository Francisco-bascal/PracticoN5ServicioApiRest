using Microsoft.EntityFrameworkCore;
using PracticoN5ServicioApiRest.Data;
using PracticoN5ServicioApiRest.Models;

namespace PracticoN5ServicioApiRest.Services
{
    public class ClientesService
    {
        private readonly SistemaVentasDbContext _contexto;

        public ClientesService(SistemaVentasDbContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<ICollection<Cliente>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        {
            return await _contexto.Clientes
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<Cliente?> ObtenerPorIdAsync(
            int id, 
            bool incluirVentas = false, 
            CancellationToken cancellationToken = default)
        {
            var consulta = _contexto.Clientes.AsNoTracking();

            if (incluirVentas)
            {
                consulta = consulta
                    .Include(c => c.Ventas)
                        .ThenInclude(v => v.Detalles)
                            .ThenInclude(d => d.Producto);
            }

            return await consulta.FirstOrDefaultAsync(c => c.ClienteId == id, cancellationToken);
        }

        public async Task<Cliente> CrearAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(cliente.Nombre))
            {
                throw new ArgumentException("El nombre del cliente es obligatorio.", nameof(cliente));
            }

            if (!string.IsNullOrWhiteSpace(cliente.Email))
            {
                bool emailDuplicado = await _contexto.Clientes
                    .AnyAsync(c => c.Email != null && c.Email.ToLower() == cliente.Email.ToLower(), cancellationToken);

                if (emailDuplicado)
                {
                    throw new InvalidOperationException($"Ya existe un cliente con el correo electrónico '{cliente.Email}'.");
                }
            }

            cliente.Ventas = new List<Venta>();
            _contexto.Clientes.Add(cliente);
            await _contexto.SaveChangesAsync(cancellationToken);

            return cliente;
        }

        public async Task<Cliente> ActualizarAsync(
            int id, 
            Cliente clienteActualizado, 
            CancellationToken cancellationToken = default)
        {
            var clienteExistente = await _contexto.Clientes
                .FirstOrDefaultAsync(c => c.ClienteId == id, cancellationToken);

            if (clienteExistente == null)
            {
                throw new KeyNotFoundException($"No se encontró el cliente con ID {id}.");
            }

            if (string.IsNullOrWhiteSpace(clienteActualizado.Nombre))
            {
                throw new ArgumentException("El nombre del cliente no puede estar vacío.", nameof(clienteActualizado));
            }

            if (!string.IsNullOrWhiteSpace(clienteActualizado.Email))
            {
                bool emailDuplicado = await _contexto.Clientes
                    .AnyAsync(c => c.ClienteId != id && c.Email != null && c.Email.ToLower() == clienteActualizado.Email.ToLower(), cancellationToken);

                if (emailDuplicado)
                {
                    throw new InvalidOperationException($"Ya existe otro cliente con el correo electrónico '{clienteActualizado.Email}'.");
                }
            }

            clienteExistente.Nombre = clienteActualizado.Nombre;
            clienteExistente.Apellido = clienteActualizado.Apellido;
            clienteExistente.Telefono = clienteActualizado.Telefono;
            clienteExistente.Email = clienteActualizado.Email;
            clienteExistente.Direccion = clienteActualizado.Direccion;

            await _contexto.SaveChangesAsync(cancellationToken);

            return clienteExistente;
        }

        public async Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default)
        {
            var cliente = await _contexto.Clientes
                .FirstOrDefaultAsync(c => c.ClienteId == id, cancellationToken);

            if (cliente == null)
            {
                throw new KeyNotFoundException($"No se encontró el cliente con ID {id}.");
            }

            bool tieneVentas = await _contexto.Ventas
                .AnyAsync(v => v.ClienteId == id, cancellationToken);

            if (tieneVentas)
            {
                throw new InvalidOperationException("No se puede eliminar el cliente porque posee ventas registradas.");
            }

            _contexto.Clientes.Remove(cliente);
            await _contexto.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
