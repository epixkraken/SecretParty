using System.ComponentModel.DataAnnotations;

namespace SecretParty.Models
{
    // Solo los datos que el usuario puede cambiar en "configura tu cuenta".
    // No incluye Email ni PasswordHash, así nadie puede cambiarlos desde este formulario.
    public class EditarPerfilViewModel
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [StringLength(30)]
        [Display(Name = "Nombre de usuario")]
        public string NombreUsuario { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "Máximo 300 caracteres")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de nacimiento")]
        public DateTime? FechaNacimiento { get; set; }

        public string? Intereses { get; set; }

        public string? Instagram { get; set; }
    }
}
