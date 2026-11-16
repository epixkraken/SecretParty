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

        // El archivo que llega desde <input type="file">. Es opcional: si no eliges nada, se queda la foto que tenías.
        [Display(Name = "Foto de perfil")]
        public IFormFile? Foto { get; set; }

        // Solo para MOSTRAR la foto actual en el formulario (no se guarda desde aquí)
        public string? FotoActual { get; set; }

        // Checkbox "quitar mi foto"
        public bool QuitarFoto { get; set; }
    }
}
