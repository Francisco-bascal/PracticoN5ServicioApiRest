using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PracticoN5ServicioApiRest.Models
{
    public class Compra
    {
        [Key]
        public int CompraId { get; set; }
        [Required]
        public DateTime Fecha { get; set; }

        [ForeignKey(nameof(Proveedor))]
        public int ProveedorId { get; set; }
        [Required]
        public Proveedor Proveedor { get; set; } = null!;

        [InverseProperty(nameof(DetalleCompra.Compra))]
        public ICollection<DetalleCompra> Detalles { get; set; } = new List<DetalleCompra>();
    }
}