using System.ComponentModel.DataAnnotations;

namespace SecretParty.Models
{
    public enum ResultadoTraza
    {
        Exito = 0,
        Falla = 1
    }

    // Una TRAZA es un registro de "qué pasó" dentro de la app.
    // Es la idea del paper "Mining design patterns from distributed execution traces":
    // ellos usan Jaeger; nosotros guardamos las trazas en nuestra propia base (SQLite)
    // para después buscar patrones (fallback, retry) con consultas SQL.
    public class Traza
    {
        // El Id también sirve de "tiempo lógico": un Id más alto = pasó después
        public int Id { get; set; }

        // Agrupa todo lo que pasó en UNA misma petición (los "hermanos" del paper)
        [Required, StringLength(64)]
        public string Peticion { get; set; } = string.Empty;

        // Lo que se quería lograr, ej: "Login", "ChatNuevos"
        [Required, StringLength(40)]
        public string Operacion { get; set; } = string.Empty;

        // Quién lo intentó, ej: "Autenticacion", "BaseDeDatos", "GoogleCalendar"
        [Required, StringLength(40)]
        public string Servicio { get; set; } = string.Empty;

        public ResultadoTraza Resultado { get; set; }

        // Si falló, por qué
        [StringLength(300)]
        public string? Detalle { get; set; }

        public long DuracionMs { get; set; }


        public int? UsuarioId { get; set; }

        public DateTime Fecha { get; set; }
    }
}