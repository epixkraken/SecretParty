using System.ComponentModel.DataAnnotations;

namespace SecretParty.Models
{
    // El formulario de "Nuevo Hang" / "Editar Hang".
    // No trae AnfitrionId, EsOficial ni Estado: eso lo decide el SERVIDOR, nunca el formulario.
    public class EventoFormViewModel
    {
        public int Id { get; set; } // 0 = evento nuevo

        [Required(ErrorMessage = "Ponle un nombre a tu hang")]
        [StringLength(60, ErrorMessage = "Máximo 60 caracteres")]
        [Display(Name = "Nombre del hang")]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "¿Cuándo es?")]
        [Display(Name = "Fecha y hora")]
        public DateTime? FechaHora { get; set; }

        [Required(ErrorMessage = "Escribe la zona o el barrio")]
        [StringLength(60, ErrorMessage = "Máximo 60 caracteres")]
        [Display(Name = "Zona")]
        public string Zona { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribe la dirección exacta")]
        [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
        [Display(Name = "Dirección exacta")]
        public string Direccion { get; set; } = string.Empty;

        [Range(1, 500, ErrorMessage = "El cupo va de 1 a 500 personas")]
        [Display(Name = "Cupo de invitados")]
        public int Cupo { get; set; } = 10;

        [Display(Name = "¿Quién puede entrar?")]
        public bool EsPublico { get; set; } = true;

        [Display(Name = "Foto de portada")]
        public IFormFile? Foto { get; set; }

        public string? FotoActual { get; set; }
        public bool QuitarFoto { get; set; }

        // La canción elegida en el buscador (van en campos escondidos que llena el JavaScript)
        [StringLength(200)] public string? CancionTitulo { get; set; }
        [StringLength(200)] public string? CancionArtista { get; set; }
        [StringLength(400)] public string? CancionPortada { get; set; }
        [StringLength(400)] public string? CancionPreview { get; set; }
    }

    // Un iconito en el mapa. A propósito NO tiene Direccion: esto viaja al navegador como JSON.
    public class PuntoMapa
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Zona { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public bool EsPublico { get; set; }
        public bool EsOficial { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    // Una tarjetita de evento en las listas (Explorar, My Hangs)
    public class EventoTarjeta
    {
        public Evento Evento { get; set; } = null!;
        public int Aceptados { get; set; }
        // Mi solicitud a este evento (null = no he pedido entrar)
        public EstadoSolicitud? MiEstado { get; set; }

        public int Libres => Math.Max(0, Evento.Cupo - Aceptados);
    }

    // La pantalla de "Explorar"
    public class ExplorarViewModel
    {
        public string? Buscar { get; set; }
        public string? Tipo { get; set; } // "publico", "privado" o null = todos
        public List<EventoTarjeta> Eventos { get; set; } = new();
    }

    // "My Hangs": los que organizo y a los que voy
    public class MisHangsViewModel
    {
        public List<EventoTarjeta> Organizo { get; set; } = new();
        public List<EventoTarjeta> Voy { get; set; } = new();
        public List<EventoTarjeta> Pendientes { get; set; } = new();
        public List<EventoTarjeta> Pasados { get; set; } = new();
    }

    // El detalle de un evento
    public class EventoDetalleViewModel
    {
        public Evento Evento { get; set; } = null!;
        public List<Usuario> Invitados { get; set; } = new(); // los aceptados
        public int PendientesCount { get; set; }
        public Solicitud? MiSolicitud { get; set; }

        public bool SoyAnfitrion { get; set; }
        public bool SoyAdmin { get; set; }

        // LA CLAVE DE LA PRIVACIDAD: el controlador decide si se puede ver la dirección.
        // Si es false, la vista nunca escribe la dirección en el HTML.
        public bool PuedeVerDireccion { get; set; }

        public bool YaPaso { get; set; }
        public int Libres => Math.Max(0, Evento.Cupo - Invitados.Count);
    }
}
