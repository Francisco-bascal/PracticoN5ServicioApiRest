using System.ComponentModel.DataAnnotations;

namespace PracticoN5ServicioApiRest.Models
{
    public class Proveedor
    {
        [Key]
        public int ProveedorId { get; set; }
        [Required]
        public string Nombre { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        public string? Direccion { get; set; }
    }
}