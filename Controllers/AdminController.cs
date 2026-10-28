using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SecretParty.Data;
using SecretParty.Models;

namespace SecretParty.Controllers
{
    // Solo entra quien tiene sesión Y el rol "Admin".
    // Sin sesión -> al login. Con sesión pero sin rol -> Acceso denegado.
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        // Con esta cantidad de flags, el perfil sale marcado "en riesgo"
        public const int LimiteReportes = 3;

        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

        // ---------- DASHBOARD ----------
        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            var haceUnaSemana = DateTime.Now.AddDays(-7);

            var model = new PanelAdminViewModel
            {
                TotalUsuarios = await _db.Usuarios.CountAsync(),
                NuevosEstaSemana = await _db.Usuarios.CountAsync(u => u.FechaRegistro >= haceUnaSemana),
                TotalReportes = await _db.Reportes.CountAsync(),
                // Agrupa los reportes por usuario y cuenta cuántos pasan el límite
                UsuariosEnRiesgo = await _db.Reportes
                    .GroupBy(r => r.UsuarioReportadoId)
                    .CountAsync(g => g.Count() >= LimiteReportes),
                UltimosReportes = await _db.Reportes
                    .Include(r => r.UsuarioReportado)
                    .Include(r => r.Motivo).ThenInclude(m => m!.Categoria)
                    .OrderByDescending(r => r.Fecha)
                    .Take(5)
                    .ToListAsync()
            };
            return View(model);
        }

        // ---------- MI CUENTA ----------
        // GET: /Admin/MiCuenta  -> los datos del admin, sin salir del panel
        public async Task<IActionResult> MiCuenta()
        {
            var miId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var admin = await _db.Usuarios.FindAsync(miId);
            if (admin == null) return RedirectToAction("Login", "Cuenta");
            return View(admin);
        }

        // ---------- USUARIOS ----------
        // GET: /Admin/Usuarios
        public async Task<IActionResult> Usuarios()
        {
            var filas = await _db.Usuarios
                .Select(u => new UsuarioAdminFila
                {
                    Usuario = u,
                    Reportes = _db.Reportes.Count(r => r.UsuarioReportadoId == u.Id)
                })
                .ToListAsync();

            // Los más reportados primero
            filas = filas.OrderByDescending(f => f.Reportes)
                         .ThenBy(f => f.Usuario.NombreUsuario)
                         .ToList();
            return View(filas);
        }

        // GET: /Admin/EditarUsuario/5  -> formulario con los datos actuales
        [HttpGet]
        public async Task<IActionResult> EditarUsuario(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null || usuario.EsAdmin) return NotFound();

            return View(new EditarUsuarioAdminViewModel
            {
                Id = usuario.Id,
                NombreUsuario = usuario.NombreUsuario,
                Email = usuario.Email,
                FechaNacimiento = usuario.FechaNacimiento
            });
        }

        // POST: /Admin/EditarUsuario/5  -> valida en el servidor y guarda
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarUsuario(EditarUsuarioAdminViewModel model)
        {
            var usuario = await _db.Usuarios.FindAsync(model.Id);
            if (usuario == null || usuario.EsAdmin) return NotFound();

            // VALIDACIÓN EN EL BACK-END del dato sensible: ni el admin puede guardar un menor de 18
            var errorEdad = Edad.Validar(model.FechaNacimiento);
            if (errorEdad != null)
                ModelState.AddModelError(nameof(model.FechaNacimiento), errorEdad);

            // El email y el nombre no pueden ser de OTRA persona
            if (await _db.Usuarios.AnyAsync(u => u.Email == model.Email && u.Id != model.Id))
                ModelState.AddModelError(nameof(model.Email), "Ya existe una cuenta con este email");
            if (await _db.Usuarios.AnyAsync(u => u.NombreUsuario == model.NombreUsuario && u.Id != model.Id))
                ModelState.AddModelError(nameof(model.NombreUsuario), "Ese nombre de usuario ya está ocupado");

            if (!ModelState.IsValid) return View(model);

            usuario.NombreUsuario = model.NombreUsuario;
            usuario.Email = model.Email;
            usuario.FechaNacimiento = model.FechaNacimiento;
            await _db.SaveChangesAsync();

            TempData["Mensaje"] = $"Se actualizó a {usuario.NombreUsuario}.";
            return RedirectToAction(nameof(Usuarios));
        }

        // GET: /Admin/EliminarUsuario/5  -> pantalla de confirmación con sus reportes
        [HttpGet]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            ViewBag.Reportes = await _db.Reportes
                .Where(r => r.UsuarioReportadoId == id)
                .Include(r => r.Motivo).ThenInclude(m => m!.Categoria)
                .OrderByDescending(r => r.Fecha)
                .ToListAsync();
            return View(usuario);
        }

        // POST: /Admin/EliminarUsuario/5  -> lo borra de verdad (y sus reportes, por la cascada)
        [HttpPost, ActionName("EliminarUsuario")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarUsuarioConfirmado(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            // Un admin no se puede borrar a sí mismo ni a otro admin desde aquí
            var miId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (usuario.Id == miId || usuario.EsAdmin)
            {
                TempData["Mensaje"] = "No puedes eliminar una cuenta de administrador.";
                return RedirectToAction(nameof(Usuarios));
            }

            _db.Usuarios.Remove(usuario);
            await _db.SaveChangesAsync();
            TempData["Mensaje"] = $"Se eliminó a {usuario.NombreUsuario}.";
            return RedirectToAction(nameof(Usuarios));
        }

        // ---------- FLAGUEAR UN PERFIL ----------
        // GET: /Admin/NuevoReporte?usuarioId=5  (usuarioId es opcional: deja ese perfil ya escogido)
        [HttpGet]
        public async Task<IActionResult> NuevoReporte(int? usuarioId)
        {
            var model = new NuevoReporteViewModel { UsuarioReportadoId = usuarioId ?? 0 };
            await CargarDropdowns(model);
            return View(model);
        }

        // POST: /Admin/NuevoReporte
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NuevoReporte(NuevoReporteViewModel model)
        {
            // Validación en el BACK-END: aunque alguien edite el HTML o mande
            // el formulario a mano, aquí revisamos que los IDs existan de verdad.
            if (!await _db.Usuarios.AnyAsync(u => u.Id == model.UsuarioReportadoId))
                ModelState.AddModelError(nameof(model.UsuarioReportadoId), "Ese perfil no existe");

            var motivo = await _db.MotivosReporte.FindAsync(model.MotivoId);
            if (motivo == null || motivo.CategoriaReporteId != model.CategoriaId)
                ModelState.AddModelError(nameof(model.MotivoId), "Ese motivo no pertenece a la categoría escogida");

            if (!ModelState.IsValid)
            {
                await CargarDropdowns(model);
                return View(model);
            }

            _db.Reportes.Add(new Reporte
            {
                UsuarioReportadoId = model.UsuarioReportadoId,
                MotivoReporteId = model.MotivoId,
                Comentario = model.Comentario
            });
            await _db.SaveChangesAsync();

            TempData["Mensaje"] = "Reporte guardado.";
            return RedirectToAction(nameof(Usuarios));
        }

        // GET: /Admin/MotivosPorCategoria?categoriaId=2
        // Lo llama el JavaScript del formulario: al cambiar la categoría,
        // pide solo los motivos de esa categoría y rellena el segundo dropdown.
        [HttpGet]
        public async Task<IActionResult> MotivosPorCategoria(int categoriaId)
        {
            var motivos = await _db.MotivosReporte
                .Where(m => m.CategoriaReporteId == categoriaId)
                .OrderBy(m => m.Nombre)
                .Select(m => new { m.Id, m.Nombre })
                .ToListAsync();
            return Json(motivos);
        }

        // Llena las listas de los 3 dropdowns
        private async Task CargarDropdowns(NuevoReporteViewModel model)
        {
            ViewBag.Usuarios = new SelectList(
                await _db.Usuarios.Where(u => !u.EsAdmin).OrderBy(u => u.NombreUsuario).ToListAsync(),
                "Id", "NombreUsuario", model.UsuarioReportadoId);

            ViewBag.Categorias = new SelectList(
                await _db.CategoriasReporte.OrderBy(c => c.Id).ToListAsync(),
                "Id", "Nombre", model.CategoriaId);

            // Si ya había categoría (ej: volvió con un error), se cargan sus motivos
            ViewBag.Motivos = new SelectList(
                await _db.MotivosReporte.Where(m => m.CategoriaReporteId == model.CategoriaId)
                    .OrderBy(m => m.Nombre).ToListAsync(),
                "Id", "Nombre", model.MotivoId);
        }
    }
}
