using System.ComponentModel.DataAnnotations;

namespace SecretParty.Models
{
    // Un enum es una lista cerrada de opciones. En la base se guarda como número (0, 1, 2).
    public enum EstadoEvento
    {
        Activo = 0,
        Cancelado = 1,   // lo canceló el anfitrión
        Oculto = 2       // lo escondió el admin (moderación)
    }

    // Un "hang": la fiesta, cena, karaoke... que organiza un usuario
    public class Evento
    {
        public int Id { get; set; }

        [Required, StringLength(60)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        public DateTime FechaHora { get; set; }

        // La zona la ve TODO el mundo (ej: "Quito · Cumbayá")
        [Required, StringLength(60)]
        public string Zona { get; set; } = string.Empty;

        // La dirección exacta es SECRETA: solo la ven el anfitrión y los invitados aceptados
        [Required, StringLength(200)]
        public string Direccion { get; set; } = string.Empty;

        // Cuántos invitados caben (sin contar al anfitrión)
        public int Cupo { get; set; }

        // true = cualquiera que pida entrar queda aceptado al instante
        // false = el anfitrión tiene que aceptar cada solicitud
        public bool EsPublico { get; set; }

        // true = lo creó el admin, es un evento oficial de Secret Party
        public bool EsOficial { get; set; }

        public string? FotoPortada { get; set; }

        // "Se escucha": la canción del hang (sale de la API de iTunes). Todo opcional.
        [StringLength(200)]
        public string? CancionTitulo { get; set; }
        [StringLength(200)]
        public string? CancionArtista { get; set; }
        [StringLength(400)]
        public string? CancionPortada { get; set; }   // link a la carátula (servidor de Apple)
        [StringLength(400)]
        public string? CancionPreview { get; set; }   // link al audio de 30 s (servidor de Apple)

        public EstadoEvento Estado { get; set; } = EstadoEvento.Activo;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Clave foránea -> Usuario (quien organiza)
        public int AnfitrionId { get; set; }
        public Usuario? Anfitrion { get; set; }

        // Un evento tiene muchas solicitudes
        public List<Solicitud> Solicitudes { get; set; } = new();
    }

    public enum EstadoSolicitud
    {
        Pendiente = 0,
        Aceptada = 1,
        Rechazada = 2
    }

    // "Quiero ir": un usuario pide entrar a un evento
    public class Solicitud
    {
        public int Id { get; set; }

        public int EventoId { get; set; }
        public Evento? Evento { get; set; }

        // Quien pide entrar
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        // Lo que le escribe al anfitrión ("hola! voy con buena vibra...")
        [StringLength(300)]
        public string? Mensaje { get; set; }

        public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;

        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
