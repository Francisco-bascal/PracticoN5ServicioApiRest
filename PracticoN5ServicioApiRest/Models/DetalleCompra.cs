using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PracticoN5ServicioApiRest.Models
{
    public class DetalleCompra
    {
        [Key]
        public int DetalleCompraId { get; set; }
        [ForeignKey("")]
        public int CompraId { get; set; }
        [ForeignKey("")]
        public int ProductoId { get; set; }
        [Required]
        public int Cantidad { get; set; }
        [Required]
        public decimal PrecioUnitario { get; set; }
    }
}