using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretParty.Data;
using SecretParty.Models;

namespace SecretParty.Controllers
{
    [Authorize] // Todo este controlador exige haber iniciado sesión
    public class PerfilController : Controller
    {
        private readonly AppDbContext _db;

        public PerfilController(AppDbContext db)
        {
            _db = db;
        }

        // ---------- VER MI PERFIL (Read) ----------
        // GET: /Perfil
        public async Task<IActionResult> Index()
        {
            var usuario = await ObtenerUsuarioActual();
            if (usuario == null) return RedirectToAction("Login", "Cuenta");
            return View(usuario);
        }

        // ---------- EDITAR MI PERFIL (Update) ----------
        // GET: /Perfil/Editar  -> muestra el formulario con mis datos actuales
        [HttpGet]
        public async Task<IActionResult> Editar()
        {
            var usuario = await ObtenerUsuarioActual();
            if (usuario == null) return RedirectToAction("Login", "Cuenta");

            var model = new EditarPerfilViewModel
            {
                NombreUsuario = usuario.NombreUsuario,
                Descripcion = usuario.Descripcion,
                FechaNacimiento = usuario.FechaNacimiento,
                Intereses = usuario.Intereses,
                Instagram = usuario.Instagram
            };
            return View(model);
        }

        // POST: /Perfil/Editar  -> guarda los cambios
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(EditarPerfilViewModel model)
        {
            var usuario = await ObtenerUsuarioActual();
            if (usuario == null) return RedirectToAction("Login", "Cuenta");

            if (!ModelState.IsValid) return View(model);

            // El nombre no puede estar ocupado por OTRA persona
            if (await _db.Usuarios.AnyAsync(u => u.NombreUsuario == model.NombreUsuario && u.Id != usuario.Id))
            {
                ModelState.AddModelError(nameof(model.NombreUsuario), "Ese nombre de usuario ya está ocupado");
                return View(model);
            }

            usuario.NombreUsuario = model.NombreUsuario;
            usuario.Descripcion = model.Descripcion;
            usuario.FechaNacimiento = model.FechaNacimiento;
            usuario.Intereses = model.Intereses;
            usuario.Instagram = model.Instagram;
            await _db.SaveChangesAsync();

            // Renovamos la cookie para que el saludo de arriba muestre el nombre nuevo
            await RenovarCookie(usuario);

            return RedirectToAction(nameof(Index));
        }

        // ---------- ELIMINAR MI CUENTA (Delete) ----------
        // GET: /Perfil/Eliminar  -> pantalla de "¿Segura?"
        [HttpGet]
        public async Task<IActionResult> Eliminar()
        {
            var usuario = await ObtenerUsuarioActual();
            if (usuario == null) return RedirectToAction("Login", "Cuenta");
            return View(usuario);
        }

        // POST: /Perfil/Eliminar  -> borra la cuenta de verdad
        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado()
        {
            var usuario = await ObtenerUsuarioActual();
            if (usuario != null)
            {
                _db.Usuarios.Remove(usuario);
                await _db.SaveChangesAsync();
            }

            // Sin cuenta ya no puede seguir con la sesión abierta
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Registro", "Cuenta");
        }

        // Busca en la base al usuario de la cookie (nunca uno que venga de la URL)
        private async Task<Usuario?> ObtenerUsuarioActual()
        {
            var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return await _db.Usuarios.FindAsync(id);
        }

        private async Task RenovarCookie(Usuario usuario)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                new Claim(ClaimTypes.Email, usuario.Email)
            };

            var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identidad));
        }
    }
}
