using System.ComponentModel.DataAnnotations;

namespace PracticoN5ServicioApiRest.Models
{
    public class Usuario
    {
        [Key]
        public int UsuarioId { get; set; }
        [Required]
        public string NombreUsuario { get; set; } = string.Empty;
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        [Required]
        public string Rol { get; set; } = string.Empty;
    }
}