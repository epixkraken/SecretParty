using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretParty.Data;
using SecretParty.Models;
using SecretParty.Servicios;

namespace SecretParty.Controllers
{
    // Todo lo de los "hangs": explorar, crear, editar, pedir entrar y aceptar solicitudes.
    [Authorize]
    public class EventosController : Controller
    {
        private readonly AppDbContext _db;
        private readonly AlmacenFotos _fotos;

        public EventosController(AppDbContext db, AlmacenFotos fotos)
        {
            _db = db;
            _fotos = fotos;
        }

        // El Id de quien está usando la app, sacado de la cookie (nunca de la URL)
        private int MiId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private bool SoyAdmin => User.IsInRole("Admin");

        // ================= EXPLORAR =================
        // GET: /Eventos?buscar=karaoke&tipo=privado
        public async Task<IActionResult> Index(string? buscar, string? tipo)
        {
            var ahora = Fechas.Ahora;

            // Solo eventos activos y que todavía no pasan
            var consulta = _db.Eventos.Where(e => e.Estado == EstadoEvento.Activo && e.FechaHora >= ahora);

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim().ToLower();
                consulta = consulta.Where(e => e.Titulo.ToLower().Contains(texto)
                                            || e.Zona.ToLower().Contains(texto)
                                            || (e.Descripcion != null && e.Descripcion.ToLower().Contains(texto)));
            }

            if (tipo == "publico") consulta = consulta.Where(e => e.EsPublico);
            else if (tipo == "privado") consulta = consulta.Where(e => !e.EsPublico);

            // Los oficiales de Secret Party van primero; después, el más próximo
            consulta = consulta.OrderByDescending(e => e.EsOficial).ThenBy(e => e.FechaHora);

            return View(new ExplorarViewModel
            {
                Buscar = buscar,
                Tipo = tipo,
                Eventos = await ATarjetas(consulta).ToListAsync()
            });
        }

        // ================= MY HANGS =================
        // GET: /Eventos/MisHangs
        public async Task<IActionResult> MisHangs()
        {
            var ahora = Fechas.Ahora;
            var miId = MiId;

            var model = new MisHangsViewModel
            {
                Organizo = await ATarjetas(_db.Eventos
                    .Where(e => e.AnfitrionId == miId && e.FechaHora >= ahora)
                    .OrderBy(e => e.FechaHora)).ToListAsync(),

                Voy = await ATarjetas(_db.Eventos
                    .Where(e => e.FechaHora >= ahora && e.Solicitudes.Any(s => s.UsuarioId == miId && s.Estado == EstadoSolicitud.Aceptada))
                    .OrderBy(e => e.FechaHora)).ToListAsync(),

                Pendientes = await ATarjetas(_db.Eventos
                    .Where(e => e.FechaHora >= ahora && e.Solicitudes.Any(s => s.UsuarioId == miId && s.Estado == EstadoSolicitud.Pendiente))
                    .OrderBy(e => e.FechaHora)).ToListAsync(),

                Pasados = await ATarjetas(_db.Eventos
                    .Where(e => e.FechaHora < ahora && (e.AnfitrionId == miId
                             || e.Solicitudes.Any(s => s.UsuarioId == miId && s.Estado == EstadoSolicitud.Aceptada)))
                    .OrderByDescending(e => e.FechaHora)
                    .Take(12)).ToListAsync()
            };
            return View(model);
        }

        // ================= DETALLE =================
        // GET: /Eventos/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            var miId = MiId;
            var evento = await _db.Eventos.Include(e => e.Anfitrion).FirstOrDefaultAsync(e => e.Id == id);
            if (evento == null) return NotFound();

            var soyAnfitrion = evento.AnfitrionId == miId;

            // Un evento oculto por el admin solo lo ven su anfitrión y el admin
            if (evento.Estado == EstadoEvento.Oculto && !soyAnfitrion && !SoyAdmin) return NotFound();

            var miSolicitud = await _db.Solicitudes.FirstOrDefaultAsync(s => s.EventoId == id && s.UsuarioId == miId);

            var model = new EventoDetalleViewModel
            {
                Evento = evento,
                MiSolicitud = miSolicitud,
                SoyAnfitrion = soyAnfitrion,
                SoyAdmin = SoyAdmin,
                YaPaso = evento.FechaHora < Fechas.Ahora,
                Invitados = await _db.Solicitudes
                    .Where(s => s.EventoId == id && s.Estado == EstadoSolicitud.Aceptada)
                    .OrderBy(s => s.Fecha)
                    .Select(s => s.Usuario!)
                    .ToListAsync(),
                PendientesCount = soyAnfitrion
                    ? await _db.Solicitudes.CountAsync(s => s.EventoId == id && s.Estado == EstadoSolicitud.Pendiente)
                    : 0,

                // LA REGLA DE PRIVACIDAD, decidida en el servidor:
                // ve la dirección el anfitrión, el admin (para moderar) o un invitado ACEPTADO.
                PuedeVerDireccion = soyAnfitrion || SoyAdmin || miSolicitud?.Estado == EstadoSolicitud.Aceptada
            };
            return View(model);
        }

        // ================= CREAR =================
        // GET: /Eventos/Crear
        [HttpGet]
        public IActionResult Crear()
        {
            // Por defecto: dentro de una semana, a las 21:00
            var model = new EventoFormViewModel { FechaHora = Fechas.Ahora.Date.AddDays(7).AddHours(21) };
            return View("Formulario", model);
        }

        // POST: /Eventos/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(EventoFormViewModel model)
        {
            ValidarFormulario(model);
            if (!ModelState.IsValid) return View("Formulario", model);

            var evento = new Evento
            {
                AnfitrionId = MiId,        // el anfitrión SIEMPRE es quien tiene la sesión
                EsOficial = SoyAdmin,      // si lo crea el admin, es oficial de Secret Party
                FechaCreacion = Fechas.Ahora
            };
            CopiarDatos(model, evento);

            if (model.Foto != null)
                evento.FotoPortada = await _fotos.Guardar(model.Foto, "eventos");

            _db.Eventos.Add(evento);
            await _db.SaveChangesAsync();

            TempData["Mensaje"] = "¡Tu hang está publicado! 🎉";
            return RedirectToAction(nameof(Detalle), new { id = evento.Id });
        }

        // ================= EDITAR =================
        // GET: /Eventos/Editar/5
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var evento = await _db.Eventos.FindAsync(id);
            if (evento == null || evento.AnfitrionId != MiId) return NotFound(); // solo el anfitrión

            return View("Formulario", new EventoFormViewModel
            {
                Id = evento.Id,
                Titulo = evento.Titulo,
                Descripcion = evento.Descripcion,
                FechaHora = evento.FechaHora,
                Zona = evento.Zona,
                Direccion = evento.Direccion,
                Cupo = evento.Cupo,
                EsPublico = evento.EsPublico,
                FotoActual = evento.FotoPortada
            });
        }

        // POST: /Eventos/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(EventoFormViewModel model)
        {
            var evento = await _db.Eventos.FindAsync(model.Id);
            if (evento == null || evento.AnfitrionId != MiId) return NotFound();

            ValidarFormulario(model);

            // No puedes bajar el cupo por debajo de la gente que ya aceptaste
            var aceptados = await _db.Solicitudes.CountAsync(s => s.EventoId == evento.Id && s.Estado == EstadoSolicitud.Aceptada);
            if (model.Cupo < aceptados)
                ModelState.AddModelError(nameof(model.Cupo), $"Ya aceptaste a {aceptados} personas, el cupo no puede ser menor");

            model.FotoActual = evento.FotoPortada;
            if (!ModelState.IsValid) return View("Formulario", model);

            var eraPrivado = !evento.EsPublico;
            CopiarDatos(model, evento);

            if (model.Foto != null)
            {
                var vieja = evento.FotoPortada;
                evento.FotoPortada = await _fotos.Guardar(model.Foto, "eventos");
                _fotos.Borrar(vieja);
            }
            else if (model.QuitarFoto)
            {
                _fotos.Borrar(evento.FotoPortada);
                evento.FotoPortada = null;
            }

            // Si lo pasaste de privado a público, los que estaban esperando entran (mientras haya cupo)
            if (eraPrivado && evento.EsPublico)
            {
                var libres = evento.Cupo - aceptados;
                var esperando = await _db.Solicitudes
                    .Where(s => s.EventoId == evento.Id && s.Estado == EstadoSolicitud.Pendiente)
                    .OrderBy(s => s.Fecha)
                    .Take(Math.Max(0, libres))
                    .ToListAsync();
                foreach (var s in esperando) s.Estado = EstadoSolicitud.Aceptada;
            }

            await _db.SaveChangesAsync();
            TempData["Mensaje"] = "Cambios guardados.";
            return RedirectToAction(nameof(Detalle), new { id = evento.Id });
        }

        // ================= CANCELAR / ELIMINAR =================
        // POST: /Eventos/Cancelar/5  -> queda visible como "cancelado" para los invitados
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var evento = await _db.Eventos.FindAsync(id);
            if (evento == null || evento.AnfitrionId != MiId) return NotFound();

            evento.Estado = EstadoEvento.Cancelado;
            await _db.SaveChangesAsync();
            TempData["Mensaje"] = "Cancelaste el hang. Tus invitados lo verán como cancelado.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        // POST: /Eventos/Eliminar/5  -> lo borra de verdad (y sus solicitudes, por la cascada)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var evento = await _db.Eventos.FindAsync(id);
            if (evento == null || evento.AnfitrionId != MiId) return NotFound();

            _fotos.Borrar(evento.FotoPortada);
            _db.Eventos.Remove(evento);
            await _db.SaveChangesAsync();
            TempData["Mensaje"] = "Hang eliminado.";
            return RedirectToAction(nameof(MisHangs));
        }

        // ================= QUIERO IR =================
        // POST: /Eventos/QuieroIr/5
        // Aquí está la regla PÚBLICO vs PRIVADO.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuieroIr(int id, string? mensaje)
        {
            var miId = MiId;
            var evento = await _db.Eventos.FindAsync(id);
            if (evento == null) return NotFound();

            // Validaciones del servidor (aunque el botón no aparezca, alguien podría mandar el POST a mano)
            string? error = null;
            if (SoyAdmin) error = "El admin modera los eventos, no se une a ellos.";
            else if (evento.AnfitrionId == miId) error = "Es tu propio hang 😉";
            else if (evento.Estado != EstadoEvento.Activo) error = "Este hang ya no está disponible.";
            else if (evento.FechaHora < Fechas.Ahora) error = "Este hang ya pasó.";
            else if (await _db.Solicitudes.AnyAsync(s => s.EventoId == id && s.UsuarioId == miId))
                error = "Ya pediste entrar a este hang.";
            else if (await _db.Solicitudes.CountAsync(s => s.EventoId == id && s.Estado == EstadoSolicitud.Aceptada) >= evento.Cupo)
                error = "Ya no hay cupo 😢";

            if (error != null)
            {
                TempData["Mensaje"] = error;
                return RedirectToAction(nameof(Detalle), new { id });
            }

            mensaje = mensaje?.Trim();
            if (mensaje?.Length > 300) mensaje = mensaje[..300];

            _db.Solicitudes.Add(new Solicitud
            {
                EventoId = id,
                UsuarioId = miId,
                Mensaje = string.IsNullOrEmpty(mensaje) ? null : mensaje,
                Fecha = Fechas.Ahora,
                // PÚBLICO -> entras directo.  PRIVADO -> el anfitrión decide.
                Estado = evento.EsPublico ? EstadoSolicitud.Aceptada : EstadoSolicitud.Pendiente
            });

            // El mensaje de la solicitud también es el PRIMER mensaje del chat con el anfitrión
            if (!string.IsNullOrEmpty(mensaje))
            {
                _db.Mensajes.Add(new Mensaje
                {
                    RemitenteId = miId,
                    DestinatarioId = evento.AnfitrionId,
                    EventoId = evento.Id,
                    Texto = mensaje,
                    Fecha = Fechas.Ahora
                });
            }
            await _db.SaveChangesAsync();

            TempData["Mensaje"] = evento.EsPublico
                ? "¡Estás dentro! 🎉 Ya puedes ver la dirección."
                : "Solicitud enviada 💌 Cuando el anfitrión te acepte, verás la dirección.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        // POST: /Eventos/YaNoVoy/5  -> retira mi solicitud (pendiente o aceptada)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YaNoVoy(int id)
        {
            var miId = MiId;
            var solicitud = await _db.Solicitudes.FirstOrDefaultAsync(s => s.EventoId == id && s.UsuarioId == miId);

            // Si te rechazaron, no puedes borrar la solicitud para volver a pedir (anti-spam)
            if (solicitud != null && solicitud.Estado != EstadoSolicitud.Rechazada)
            {
                _db.Solicitudes.Remove(solicitud);
                await _db.SaveChangesAsync();
                TempData["Mensaje"] = "Listo, ya no estás en la lista.";
            }
            return RedirectToAction(nameof(Detalle), new { id });
        }

        // ================= SOLICITUDES (para el anfitrión) =================
        // GET: /Eventos/Solicitudes  -> todas las solicitudes pendientes de MIS hangs
        public async Task<IActionResult> Solicitudes()
        {
            var miId = MiId;
            var ahora = Fechas.Ahora;
            var pendientes = await _db.Solicitudes
                .Include(s => s.Usuario)
                .Include(s => s.Evento)
                .Where(s => s.Evento!.AnfitrionId == miId
                         && s.Estado == EstadoSolicitud.Pendiente
                         && s.Evento.FechaHora >= ahora
                         && s.Evento.Estado == EstadoEvento.Activo)
                .OrderBy(s => s.Evento!.FechaHora).ThenBy(s => s.Fecha)
                .ToListAsync();
            return View(pendientes);
        }

        // POST: /Eventos/Responder/12?aceptar=true
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Responder(int id, bool aceptar, bool volverAlEvento = false)
        {
            var solicitud = await _db.Solicitudes
                .Include(s => s.Evento)
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(s => s.Id == id);

            // Solo el anfitrión de ESE evento puede responder
            if (solicitud == null || solicitud.Evento!.AnfitrionId != MiId) return NotFound();

            if (solicitud.Estado == EstadoSolicitud.Pendiente)
            {
                if (aceptar)
                {
                    var aceptados = await _db.Solicitudes.CountAsync(s => s.EventoId == solicitud.EventoId && s.Estado == EstadoSolicitud.Aceptada);
                    if (aceptados >= solicitud.Evento.Cupo)
                    {
                        TempData["Mensaje"] = "Ya llenaste el cupo. Súbelo en Editar si quieres aceptar a más gente.";
                    }
                    else
                    {
                        solicitud.Estado = EstadoSolicitud.Aceptada;
                        TempData["Mensaje"] = $"Aceptaste a {solicitud.Usuario!.NombreUsuario} 🎉 Ya puede ver la dirección.";

                        // Aviso automático por chat, para que la invitada se entere
                        _db.Mensajes.Add(new Mensaje
                        {
                            RemitenteId = MiId,
                            DestinatarioId = solicitud.UsuarioId,
                            EventoId = solicitud.EventoId,
                            Texto = $"¡Te acepté en «{solicitud.Evento.Titulo}»! 🎉 Ya puedes ver la dirección en el hang.",
                            Fecha = Fechas.Ahora
                        });
                    }
                }
                else
                {
                    solicitud.Estado = EstadoSolicitud.Rechazada;
                    TempData["Mensaje"] = $"Rechazaste a {solicitud.Usuario!.NombreUsuario}.";
                }
                await _db.SaveChangesAsync();
            }

            return volverAlEvento
                ? RedirectToAction(nameof(Detalle), new { id = solicitud.EventoId })
                : RedirectToAction(nameof(Solicitudes));
        }

        // ================= AYUDANTES =================

        // Convierte una consulta de eventos en tarjetas, contando los aceptados
        // y viendo si YO ya pedí entrar. Todo en UNA sola consulta SQL.
        private IQueryable<EventoTarjeta> ATarjetas(IQueryable<Evento> consulta)
        {
            var miId = MiId;
            return consulta.Select(e => new EventoTarjeta
            {
                Evento = e,
                Aceptados = e.Solicitudes.Count(s => s.Estado == EstadoSolicitud.Aceptada),
                MiEstado = e.Solicitudes
                    .Where(s => s.UsuarioId == miId)
                    .Select(s => (EstadoSolicitud?)s.Estado)
                    .FirstOrDefault()
            });
        }

        // Reglas que no se pueden expresar solo con [Required] / [Range]
        private void ValidarFormulario(EventoFormViewModel model)
        {
            if (model.FechaHora != null && model.FechaHora <= Fechas.Ahora)
                ModelState.AddModelError(nameof(model.FechaHora), "La fecha tiene que ser en el futuro");

            if (model.FechaHora > Fechas.Ahora.AddYears(1))
                ModelState.AddModelError(nameof(model.FechaHora), "Solo puedes crear hangs de aquí a un año");

            if (model.Foto != null)
            {
                var errorFoto = _fotos.Validar(model.Foto);
                if (errorFoto != null)
                    ModelState.AddModelError(nameof(model.Foto), errorFoto);
            }
        }

        private static void CopiarDatos(EventoFormViewModel model, Evento evento)
        {
            evento.Titulo = model.Titulo.Trim();
            evento.Descripcion = model.Descripcion?.Trim();
            evento.FechaHora = model.FechaHora!.Value;
            evento.Zona = model.Zona.Trim();
            evento.Direccion = model.Direccion.Trim();
            evento.Cupo = model.Cupo;
            evento.EsPublico = model.EsPublico;
        }
    }
}
