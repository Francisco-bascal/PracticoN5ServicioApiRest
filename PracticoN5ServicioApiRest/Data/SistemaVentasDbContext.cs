using Microsoft.EntityFrameworkCore;
using PracticoN5ServicioApiRest.Models;

namespace PracticoN5ServicioApiRest.Data
{
    public class SistemaVentasDbContext : DbContext
    {
        public SistemaVentasDbContext(DbContextOptions<SistemaVentasDbContext> options) : base(options) { }

        public DbSet<CategoriaProducto> Categorias { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Compra> Compras { get; set; }
        public DbSet<DetalleCompra> DetallesCompra { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetallesVenta { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
