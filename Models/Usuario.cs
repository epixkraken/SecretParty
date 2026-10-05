using System.ComponentModel.DataAnnotations;

namespace SecretParty.Models
{
    public class Usuario
    {
        
    public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [StringLength(30)]
        [Display(Name = "Nombre de usuario")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Email no válido")]
        public string Email { get; set; } = string.Empty;

        // Nunca guardamos la contraseña real, solo su "hash" (versión encriptada)
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de nacimiento")]
        public DateTime? FechaNacimiento { get; set; }

        public string? Intereses { get; set; }

        public string? Instagram { get; set; }

        // Aquí guardamos la RUTA de la foto, ej: /uploads/foto123.jpg
        public string? FotoPerfil { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}