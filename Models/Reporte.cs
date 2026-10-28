using System.ComponentModel.DataAnnotations;

namespace SecretParty.Models
{
    // Tabla padre: el tipo general de problema (ej: "Perfil falso")
    public class CategoriaReporte
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        // Una categoría tiene muchos motivos
        public List<MotivoReporte> Motivos { get; set; } = new();
    }

    // Tabla hija: el motivo concreto (ej: "Fotos robadas"), pertenece a UNA categoría
    public class MotivoReporte
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Nombre { get; set; } = string.Empty;

        // Clave foránea -> CategoriaReporte
        public int CategoriaReporteId { get; set; }
        public CategoriaReporte? Categoria { get; set; }
    }

    // Un "flag": a quién se reportó y por qué
    public class Reporte
    {
        public int Id { get; set; }

        // Clave foránea -> Usuario (el perfil reportado)
        public int UsuarioReportadoId { get; set; }
        public Usuario? UsuarioReportado { get; set; }

        // Clave foránea -> MotivoReporte (de ahí sale también la categoría)
        public int MotivoReporteId { get; set; }
        public MotivoReporte? Motivo { get; set; }

        [StringLength(300)]
        public string? Comentario { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
