using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PracticoN5ServicioApiRest.Models
{
    public class Venta
    {
        [Key]
        public int VentaId { get; set; }
        [Required]
        public DateTime Fecha { get; set; }
        


        [Required]
        [ForeignKey(nameof(Cliente))]
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        [InverseProperty(nameof(DetalleVenta.Venta))]
        public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    }
}