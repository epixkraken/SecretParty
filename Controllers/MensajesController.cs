using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SecretParty.Data;
using SecretParty.Models;
using SecretParty.Servicios;

namespace SecretParty.Controllers
{
    // El chat entre anfitriones e invitados.
    [Authorize]
    public class MensajesController : Controller
    {
        public const int LargoMaximo = 1000;

        private readonly AppDbContext _db;
        private readonly Resiliencia _resiliencia;

        public MensajesController(AppDbContext db, Resiliencia resiliencia)
        {
            _db = db;
            _resiliencia = resiliencia;
        }

        private int MiId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // El admin no chatea: lo mandamos a su panel (igual que en PerfilController)
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (User.IsInRole("Admin"))
                context.Result = RedirectToAction("Index", "Admin");
        }

        // ================= BANDEJA =================
        // GET: /Mensajes
        public async Task<IActionResult> Index()
        {
            var yo = MiId;

            // Traemos todos mis mensajes (enviados y recibidos), el más nuevo primero
            var mios = await _db.Mensajes
                .Where(m => m.RemitenteId == yo || m.DestinatarioId == yo)
                .OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id)
                .ToListAsync();

            // Los agrupamos por "la otra persona": cada grupo es una conversación
            var grupos = mios
                .GroupBy(m => m.RemitenteId == yo ? m.DestinatarioId : m.RemitenteId)
                .ToList();

            var idsOtros = grupos.Select(g => g.Key).ToList();
            var usuarios = await _db.Usuarios.Where(u => idsOtros.Contains(u.Id)).ToDictionaryAsync(u => u.Id);

            var model = new BandejaViewModel
            {
                Conversaciones = grupos
                    .Where(g => usuarios.ContainsKey(g.Key))
                    .Select(g => new ConversacionFila
                    {
                        Otro = usuarios[g.Key],
                        Ultimo = g.First(), // ya venían ordenados del más nuevo al más viejo
                        UltimoEsMio = g.First().RemitenteId == yo,
                        NoLeidos = g.Count(m => m.DestinatarioId == yo && !m.Leido)
                    })
                    .ToList()
            };

            // Contactos: con quién PUEDO hablar (por un hang en común) pero aún no hay mensajes
            var contactos = await ContactosPosibles(yo);
            model.Contactos = contactos.Where(u => !usuarios.ContainsKey(u.Id)).ToList();

            return View(model);
        }

        // ================= UNA CONVERSACIÓN =================
        // GET: /Mensajes/Chat/5   (5 = el Id de la otra persona)
        public async Task<IActionResult> Chat(int id)
        {
            var yo = MiId;
            if (id == yo) return RedirectToAction(nameof(Index));

            var otro = await _db.Usuarios.FindAsync(id);
            if (otro == null || otro.EsAdmin) return NotFound();

            var mensajes = await _db.Mensajes
                .Include(m => m.Evento)
                .Where(m => (m.RemitenteId == yo && m.DestinatarioId == id) || (m.RemitenteId == id && m.DestinatarioId == yo))
                .OrderBy(m => m.Fecha).ThenBy(m => m.Id)
                .ToListAsync();

            var puede = await PuedenChatear(yo, id);

            // Sin historial y sin permiso = no tienes nada que hacer aquí
            if (mensajes.Count == 0 && !puede) return NotFound();

            // Al abrir el chat, lo que me mandó queda como leído
            await MarcarLeidos(de: id, para: yo);

            return View(new ChatViewModel
            {
                Otro = otro,
                Mensajes = mensajes,
                MiId = yo,
                PuedeEscribir = puede,
                EventoEnComun = await EventoEnComun(yo, id)
            });
        }

        // POST: /Mensajes/Enviar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enviar(int id, string? texto)
        {
            var yo = MiId;
            texto = texto?.Trim();

            // Validaciones del servidor
            if (string.IsNullOrEmpty(texto))
                return RedirectToAction(nameof(Chat), new { id });

            if (texto.Length > LargoMaximo)
            {
                TempData["Mensaje"] = $"El mensaje es muy largo (máximo {LargoMaximo} caracteres).";
                return RedirectToAction(nameof(Chat), new { id });
            }

            if (!await PuedenChatear(yo, id))
            {
                TempData["Mensaje"] = "No puedes escribirle a esta persona: no tienen un hang en común.";
                return RedirectToAction(nameof(Index));
            }

            _db.Mensajes.Add(new Mensaje
            {
                RemitenteId = yo,
                DestinatarioId = id,
                Texto = texto,
                Fecha = Fechas.Ahora
            });
            await _db.SaveChangesAsync();

            // "#fin" hace que el navegador baje hasta el último mensaje
            return Redirect(Url.Action(nameof(Chat), new { id }) + "#fin");
        }

        // GET: /Mensajes/Nuevos/5?despuesDe=120
        // Lo llama el JavaScript del chat cada pocos segundos: devuelve JSON con los mensajes nuevos.
        // Así el chat se actualiza solo, sin recargar la página.
        [HttpGet]
        public async Task<IActionResult> Nuevos(int id, int despuesDe)
        {
            var yo = MiId;
            var sinConexion = false;

            // RETRY + FALLBACK: intenta leer la base hasta 3 veces.
            // Si no se puede, plan B = responder "no hay nada nuevo" y avisarle al navegador.
            var nuevos = await _resiliencia.EjecutarAsync("ChatNuevos",
                "BaseDeDatos", () => _db.Mensajes
                    .Where(m => m.Id > despuesDe
                             && ((m.RemitenteId == yo && m.DestinatarioId == id) || (m.RemitenteId == id && m.DestinatarioId == yo)))
                    .OrderBy(m => m.Id)
                    .Select(m => new MensajeNuevo(m.Id, m.Texto, m.RemitenteId == yo, m.Fecha.ToString("HH:mm")))
                    .ToListAsync(),
                "ModoSinConexion", () =>
                {
                    sinConexion = true;
                    return Task.FromResult(new List<MensajeNuevo>());
                });

            if (sinConexion)
                Response.Headers["X-Sin-Conexion"] = "1"; // el JavaScript lee esto y muestra el aviso
            else if (nuevos.Any(m => !m.Mio))
                await MarcarLeidos(de: id, para: yo);

            return Json(nuevos);
        }

        // ================= REGLAS =================

        // Dos personas pueden chatear si una es ANFITRIONA de un hang y la otra pidió entrar
        // (solicitud pendiente o aceptada). Si la rechazaron, ya no.
        private async Task<bool> PuedenChatear(int yo, int otro)
        {
            if (yo == otro) return false;
            if (await _db.Usuarios.AnyAsync(u => u.Id == otro && u.EsAdmin)) return false;

            return await _db.Solicitudes.AnyAsync(s =>
                s.Estado != EstadoSolicitud.Rechazada
                && ((s.UsuarioId == yo && s.Evento!.AnfitrionId == otro)
                 || (s.UsuarioId == otro && s.Evento!.AnfitrionId == yo)));
        }

        // Todas las personas con las que podría hablar
        private async Task<List<Usuario>> ContactosPosibles(int yo)
        {
            // Gente que pidió entrar a MIS hangs
            var invitados = _db.Solicitudes
                .Where(s => s.Estado != EstadoSolicitud.Rechazada && s.Evento!.AnfitrionId == yo)
                .Select(s => s.Usuario!);

            // Anfitriones de los hangs a los que YO pedí entrar
            var anfitriones = _db.Solicitudes
                .Where(s => s.Estado != EstadoSolicitud.Rechazada && s.UsuarioId == yo)
                .Select(s => s.Evento!.Anfitrion!);

            var todos = await invitados.Union(anfitriones).ToListAsync();
            return todos.Where(u => !u.EsAdmin && u.Id != yo)
                        .DistinctBy(u => u.Id)
                        .OrderBy(u => u.NombreUsuario)
                        .ToList();
        }

        // El nombre del hang que tienen en común (para mostrar "sobre: Karaoke secreto")
        private async Task<string?> EventoEnComun(int yo, int otro)
        {
            return await _db.Solicitudes
                .Where(s => (s.UsuarioId == yo && s.Evento!.AnfitrionId == otro)
                         || (s.UsuarioId == otro && s.Evento!.AnfitrionId == yo))
                .OrderByDescending(s => s.Evento!.FechaHora)
                .Select(s => s.Evento!.Titulo)
                .FirstOrDefaultAsync();
        }

        private async Task MarcarLeidos(int de, int para)
        {
            // ExecuteUpdateAsync hace un UPDATE directo en la base, sin cargar los mensajes
            await _db.Mensajes
                .Where(m => m.RemitenteId == de && m.DestinatarioId == para && !m.Leido)
                .ExecuteUpdateAsync(set => set.SetProperty(m => m.Leido, true));
        }
    }
}
