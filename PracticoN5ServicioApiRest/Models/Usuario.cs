using System.ComponentModel.DataAnnotations;

namespace PracticoN5ServicioApiRest.Models
{
    public class Usuario
    {
        [Key]
        public int UsuarioId { get; set; }
        [Required]
        [Length(4, 50)]
        public string NombreUsuario { get; set; } = string.Empty;
        [EmailAddress, Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        [Required]
        [Length(3, 30)]
        public string Rol { get; set; } = string.Empty;
    }
}