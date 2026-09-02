using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PracticoN5ServicioApiRest.Models
{
    public class Producto
    {
        [Key]
        public int ProductoId { get; set; }
        [Required]
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        [Required]
        public decimal Precio { get; set; }
        [Required]
        public int Stock { get; set; }
        public string? ImagenRuta { get; set; }

        [ForeignKey(nameof(Categoria))]
        public int CategoriaId { get; set; }
        [Required]
        public virtual CategoriaProducto Categoria { get; set; } = null!;
    }
}