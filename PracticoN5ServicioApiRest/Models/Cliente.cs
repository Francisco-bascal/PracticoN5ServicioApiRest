using System.ComponentModel.DataAnnotations;

namespace PracticoN5ServicioApiRest.Models
{
    public class Cliente
    {
        [Key]
        public int ClienteId { get; set; }
        [Required]
        public string Nombre { get; set; } = string.Empty;
        public string? Apellido { get; set; }
        public string? Telefono { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        public string? Direccion { get; set; }
    }
}