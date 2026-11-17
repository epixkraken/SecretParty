using System.ComponentModel.DataAnnotations;

namespace SecretParty.Models
{
    // Un mensaje del chat entre dos usuarios
    public class Mensaje
    {
        public int Id { get; set; }

        // Quien lo escribe
        public int RemitenteId { get; set; }
        public Usuario? Remitente { get; set; }

        // Quien lo recibe
        public int DestinatarioId { get; set; }
        public Usuario? Destinatario { get; set; }

        // Opcional: el hang del que están hablando
        public int? EventoId { get; set; }
        public Evento? Evento { get; set; }

        [Required, StringLength(1000)]
        public string Texto { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        // false = el destinatario todavía no lo abre (sale el puntito lila)
        public bool Leido { get; set; }
    }

    // Una fila de la bandeja: la conversación con una persona
    public class ConversacionFila
    {
        public Usuario Otro { get; set; } = null!;
        public Mensaje Ultimo { get; set; } = null!;
        public int NoLeidos { get; set; }
        public bool UltimoEsMio { get; set; }
    }

    public class BandejaViewModel
    {
        public List<ConversacionFila> Conversaciones { get; set; } = new();
        // Gente con la que puedo hablar pero todavía no hablamos
        public List<Usuario> Contactos { get; set; } = new();
    }

    public class ChatViewModel
    {
        public Usuario Otro { get; set; } = null!;
        public List<Mensaje> Mensajes { get; set; } = new();
        public int MiId { get; set; }
        public bool PuedeEscribir { get; set; }
        public string? EventoEnComun { get; set; }
    }
    
    // Lo que el chat le manda al JavaScript cada pocos segundos
    public record MensajeNuevo(int Id, string Texto, bool Mio, string Hora);
}
