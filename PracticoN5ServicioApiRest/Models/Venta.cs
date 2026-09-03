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

        [ForeignKey(nameof(Cliente))]
        public int ClienteId { get; set; }
        [Required]
        public Cliente Cliente { get; set; } = null!;
        
        [Required, InverseProperty(nameof(DetalleVenta.Producto))]
        public ICollection<Producto> Productos { get; set; } = null!;

        [Required, InverseProperty(nameof(DetalleVenta.Venta))]
        public ICollection<DetalleVenta> Detalles { get; set; } = null!;
    }
}

//Verificar este modelado