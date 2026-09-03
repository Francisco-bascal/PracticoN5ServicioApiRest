using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PracticoN5ServicioApiRest.Models
{
    public class Cliente
    {
        [Key]
        public int ClienteId { get; set; }
        [Required]
        [Length(2, 100)]
        public string Nombre { get; set; } = string.Empty;
        [Length(2, 100)]
        public string? Apellido { get; set; }
        [Length(7,20)]
        public string? Telefono { get; set; }
        [EmailAddress]
        [MaxLength(150)]
        public string? Email { get; set; }
        [Length(3,200)]
        public string? Direccion { get; set; }

        [InverseProperty(nameof(Venta.Cliente))]
        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
    }
}