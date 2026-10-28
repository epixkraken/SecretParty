using System.ComponentModel.DataAnnotations;

namespace SecretParty.Models
{
    // Los números del dashboard del admin
    public class PanelAdminViewModel
    {
        public int TotalUsuarios { get; set; }
        public int NuevosEstaSemana { get; set; }
        public int TotalReportes { get; set; }
        public int UsuariosEnRiesgo { get; set; }
        public List<Reporte> UltimosReportes { get; set; } = new();
    }

    // Una fila de la lista de usuarios, con cuántos flags tiene
    public class UsuarioAdminFila
    {
        public Usuario Usuario { get; set; } = null!;
        public int Reportes { get; set; }
    }

    // Lo que el admin puede corregir de un usuario.
    // No incluye PasswordHash ni EsAdmin: eso nunca se toca desde un formulario.
    public class EditarUsuarioAdminViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [StringLength(30)]
        [Display(Name = "Nombre de usuario")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Email no válido")]
        public string Email { get; set; } = string.Empty;

        // Sin [Required]: la edad la valida el servidor (ver Edad.cs)
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de nacimiento")]
        public DateTime? FechaNacimiento { get; set; }
    }

    // El formulario de "flaguear un perfil".
    // Solo viajan IDs: el usuario los ESCOGE de dropdowns, nunca los escribe.
    public class NuevoReporteViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Escoge el perfil a reportar")]
        [Display(Name = "Perfil reportado")]
        public int UsuarioReportadoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Escoge una categoría")]
        [Display(Name = "Categoría")]
        public int CategoriaId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Escoge un motivo")]
        [Display(Name = "Motivo")]
        public int MotivoId { get; set; }

        [StringLength(300, ErrorMessage = "Máximo 300 caracteres")]
        [Display(Name = "Comentario (opcional)")]
        public string? Comentario { get; set; }
    }
}
