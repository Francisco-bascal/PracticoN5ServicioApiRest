using System.ComponentModel.DataAnnotations;

namespace PracticoN5ServicioApiRest.Models
{
    public class Proveedor
    {
        [Key]
        public int ProveedorId { get; set; }
        [Required]
        [Length(2, 100)]
        public string Nombre { get; set; } = string.Empty;
        [Length(7,20)]
        public string? Telefono { get; set; }
        [EmailAddress]
        [MaxLength(150)]
        public string? Email { get; set; }
        [Length(3, 200)]
        public string? Direccion { get; set; }
    }
}