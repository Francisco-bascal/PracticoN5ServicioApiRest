using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PracticoN5ServicioApiRest.Models
{
    public class DetalleVenta
    {
        [Key]
        public int DetalleVentaId { get; set; }
        [Required]
        public int Cantidad { get; set; }
        [Required]
        public decimal PrecioUnitario { get; set; }

        [ForeignKey(nameof(Venta))]
        public int VentaId { get; set; }
        [Required]
        public Venta Venta { get; set; } = null!;

        [ForeignKey(nameof(Producto))]
        public int ProductoId { get; set; }
        [Required]
        public Producto Producto { get; set; } = null!;
    }
}

//Continuar con el modelado de relaciones desde acá