namespace SecretParty.Models
{
    // Qué papel tengo yo en un evento del calendario (define el color del puntito)
    public enum RolEnEvento
    {
        Organizo,   // lila
        Voy,        // verde
        Pendiente   // amarillo: pedí entrar y todavía no me responden
    }

    public class EventoCalendario
    {
        public Evento Evento { get; set; } = null!;
        public RolEnEvento Rol { get; set; }
    }

    public class CalendarioViewModel
    {
        public int Anio { get; set; }
        public int Mes { get; set; }

        // El primer día del mes (ej: 1 de noviembre de 2026)
        public DateTime PrimerDia => new(Anio, Mes, 1);

        // El lunes con el que empieza la cuadrícula (puede ser del mes anterior)
        public DateTime InicioCuadricula
        {
            get
            {
                // DayOfWeek: domingo=0, lunes=1... Lo convertimos a lunes=0 ... domingo=6
                int diasDesdeLunes = ((int)PrimerDia.DayOfWeek + 6) % 7;
                return PrimerDia.AddDays(-diasDesdeLunes);
            }
        }

        // Cuántas semanas (filas) necesita el mes: 4, 5 o 6
        public int Semanas
        {
            get
            {
                var ultimoDia = PrimerDia.AddMonths(1).AddDays(-1);
                return (int)Math.Ceiling(((ultimoDia - InicioCuadricula).Days + 1) / 7.0);
            }
        }

        public DateTime Hoy { get; set; }
        public List<EventoCalendario> Eventos { get; set; } = new();

        public List<EventoCalendario> DelDia(DateTime dia) =>
            Eventos.Where(e => e.Evento.FechaHora.Date == dia.Date).ToList();
    }
}
