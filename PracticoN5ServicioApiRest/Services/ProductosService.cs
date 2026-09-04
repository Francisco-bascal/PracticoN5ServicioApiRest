using Microsoft.EntityFrameworkCore;
using PracticoN5ServicioApiRest.Data;
using PracticoN5ServicioApiRest.Models;

namespace PracticoN5ServicioApiRest.Services
{
    public class ProductosService
    {
        private readonly SistemaVentasDbContext _contexto;
        public ProductosService(SistemaVentasDbContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<ICollection<Producto>> GetProductosAsync() 
        {
            return(await _contexto.Productos.ToListAsync());
        }


    }
}
