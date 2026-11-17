using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SecretParty.Data;
using SecretParty.Models;
using SecretParty.Servicios;

namespace SecretParty.Controllers
{
    // "Mi calendario": los hangs que organizo y a los que me aceptaron, mes por mes.
    [Authorize]
    public class CalendarioController : Controller
    {
        private readonly AppDbContext _db;
        private readonly Resiliencia _resiliencia;

        public CalendarioController(AppDbContext db, Resiliencia resiliencia)
        {
            _db = db;
            _resiliencia = resiliencia;
        }

        private int MiId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (User.IsInRole("Admin"))
                context.Result = RedirectToAction("Index", "Admin");
        }

        // GET: /Calendario?anio=2026&mes=12   (sin parámetros = el mes actual)
        public async Task<IActionResult> Index(int? anio, int? mes)
        {
            var hoy = Fechas.Ahora.Date;
            var model = new CalendarioViewModel
            {
                Anio = anio ?? hoy.Year,
                Mes = mes is >= 1 and <= 12 ? mes.Value : hoy.Month,
                Hoy = hoy
            };
            if (model.Anio < 2000 || model.Anio > 2100) model.Anio = hoy.Year;

            // Traemos los eventos de TODA la cuadrícula (incluye los días grises del mes anterior/siguiente)
            var desde = model.InicioCuadricula;
            var hasta = desde.AddDays(model.Semanas * 7);
            var yo = MiId;

            var organizo = await _db.Eventos
                .Where(e => e.AnfitrionId == yo && e.FechaHora >= desde && e.FechaHora < hasta)
                .Select(e => new EventoCalendario { Evento = e, Rol = RolEnEvento.Organizo })
                .ToListAsync();

            // A los que pedí entrar: aceptada = "voy", pendiente = "pendiente". Los ocultos por el admin no salen.
            var invitada = await _db.Solicitudes
                .Where(s => s.UsuarioId == yo
                         && s.Estado != EstadoSolicitud.Rechazada
                         && s.Evento!.Estado != EstadoEvento.Oculto
                         && s.Evento.FechaHora >= desde && s.Evento.FechaHora < hasta)
                .Select(s => new EventoCalendario
                {
                    Evento = s.Evento!,
                    Rol = s.Estado == EstadoSolicitud.Aceptada ? RolEnEvento.Voy : RolEnEvento.Pendiente
                })
                .ToListAsync();

            model.Eventos = organizo.Concat(invitada).OrderBy(e => e.Evento.FechaHora).ToList();
            return View(model);
        }

        // GET: /Calendario/Ics/5
        // Descarga un archivo .ics: el formato estándar que entienden Google Calendar,
        // el calendario del iPhone, Outlook... Al abrirlo, el hang se agrega a tu calendario.
        public async Task<IActionResult> Ics(int id)
        {
            var yo = MiId;
            var evento = await _db.Eventos.FindAsync(id);
            if (evento == null) return NotFound();

            // Solo el anfitrión o un invitado ACEPTADO (el archivo trae la dirección secreta)
            var puede = evento.AnfitrionId == yo
                || await _db.Solicitudes.AnyAsync(s => s.EventoId == id && s.UsuarioId == yo && s.Estado == EstadoSolicitud.Aceptada);
            if (!puede) return NotFound();

            var url = Url.Action("Detalle", "Eventos", new { id }, Request.Scheme);

            // El formato .ics es texto plano con líneas CLAVE:valor
            var ics = new StringBuilder();
            ics.AppendLine("BEGIN:VCALENDAR");
            ics.AppendLine("VERSION:2.0");
            ics.AppendLine("PRODID:-//Secret Party//ES");
            ics.AppendLine("BEGIN:VEVENT");
            ics.AppendLine($"UID:evento-{evento.Id}@secretparty");
            ics.AppendLine($"DTSTAMP:{AUtc(Fechas.Ahora)}");
            ics.AppendLine($"DTSTART:{AUtc(evento.FechaHora)}");
            ics.AppendLine($"DTEND:{AUtc(evento.FechaHora.AddHours(4))}"); // duración por defecto: 4 horas
            ics.AppendLine($"SUMMARY:{Escapar(evento.Titulo)}");
            ics.AppendLine($"LOCATION:{Escapar(evento.Direccion + " (" + evento.Zona + ")")}");
            ics.AppendLine($"DESCRIPTION:{Escapar((evento.Descripcion ?? "") + "\n\n" + url)}");
            ics.AppendLine($"URL:{url}");
            ics.AppendLine("BEGIN:VALARM");             // recordatorio 2 horas antes
            ics.AppendLine("TRIGGER:-PT2H");
            ics.AppendLine("ACTION:DISPLAY");
            ics.AppendLine($"DESCRIPTION:{Escapar(evento.Titulo)}");
            ics.AppendLine("END:VALARM");
            ics.AppendLine("END:VEVENT");
            ics.AppendLine("END:VCALENDAR");

            var bytes = Encoding.UTF8.GetBytes(ics.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n"));
            return File(bytes, "text/calendar", $"secret-party-{evento.Id}.ics");
        }

                // GET: /Calendario/Google/5
        // Abre Google Calendar con el hang ya llenado.
        // FALLBACK: si Google "no responde" (modo caos), plan B = bajar el archivo .ics
        public async Task<IActionResult> Google(int id)
        {
            var yo = MiId;
            var evento = await _db.Eventos.FindAsync(id);
            if (evento == null) return NotFound();

            // Solo el anfitrión o un invitado ACEPTADO (el link trae la dirección secreta)
            var puede = evento.AnfitrionId == yo
                || await _db.Solicitudes.AnyAsync(s => s.EventoId == id && s.UsuarioId == yo && s.Estado == EstadoSolicitud.Aceptada);
            if (!puede) return NotFound();

            var urlDetalle = Url.Action("Detalle", "Eventos", new { id }, Request.Scheme)!;

            var link = await _resiliencia.EjecutarAsync<string?>("AgregarCalendario",
                "GoogleCalendar", () => Task.FromResult<string?>(LinkGoogle(evento, urlDetalle)),
                "ArchivoIcs", () => Task.FromResult<string?>(null));

            // Si hay link, vamos a Google; si no, plan B: descargar el .ics
            return link != null ? Redirect(link) : RedirectToAction(nameof(Ics), new { id });
        }
        
        // Link que abre Google Calendar con el evento ya llenado (el usuario solo toca "Guardar")
        public static string LinkGoogle(Evento e, string urlDetalle)
        {
            var datos = new Dictionary<string, string>
            {
                ["action"] = "TEMPLATE",
                ["text"] = e.Titulo,
                ["dates"] = $"{AUtc(e.FechaHora)}/{AUtc(e.FechaHora.AddHours(4))}",
                ["details"] = $"{e.Descripcion}\n\n{urlDetalle}",
                ["location"] = $"{e.Direccion} ({e.Zona})"
            };
            // Uri.EscapeDataString convierte espacios, tildes, etc. en %20, %C3%A1... para que viajen bien en la URL
            return "https://calendar.google.com/calendar/render?" +
                   string.Join("&", datos.Select(d => $"{d.Key}={Uri.EscapeDataString(d.Value)}"));
        }

        // Ecuador es UTC-5 todo el año (no tiene horario de verano), así que sumamos 5 horas
        // y escribimos la hora en UTC con una "Z" al final: 20261206T020000Z
        private static string AUtc(DateTime horaEcuador) =>
            horaEcuador.AddHours(5).ToString("yyyyMMdd'T'HHmmss'Z'");

        // En .ics, las comas, punto y coma y saltos de línea llevan "\" delante
        private static string Escapar(string texto) =>
            texto.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r", "").Replace("\n", "\\n");
    }
}
