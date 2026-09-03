using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PracticoN5ServicioApiRest.Models
{
    public class CategoriaProducto
    {
        [Key]
        public int CategoriaId { get; set; }
        [Required]
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }

        [Required, InverseProperty(nameof(Producto.Categoria))]
        public ICollection<Producto> Productos { get; set; } = null!;
    }
}