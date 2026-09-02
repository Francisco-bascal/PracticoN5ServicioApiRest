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
        [ForeignKey("")]
        public int ProveedorId { get; set; }
    }
}