using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PracticoN5ServicioApiRest.Models
{
    public class DetalleCompra
    {
        [Key]
        public int DetalleCompraId { get; set; }
        [Required]
        public int Cantidad { get; set; }
        [Required]
        public decimal PrecioUnitario { get; set; }

        [ForeignKey(nameof(Compra))]
        public int CompraId { get; set; }
        [Required]
        public Compra Compra { get; set; } = null!;

        [ForeignKey(nameof(Producto))]
        public int ProductoId { get; set; }
        [Required]
        public Producto Producto { get; set; } = null!;
    }
}