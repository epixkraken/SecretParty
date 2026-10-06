using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretParty.Data;
using SecretParty.Models;

namespace SecretParty.Controllers
{
    public class CuentaController : Controller
    {
        private readonly AppDbContext _db;
        private readonly PasswordHasher<Usuario> _hasher = new();

        public CuentaController(AppDbContext db)
        {
            _db = db;
        }

        // ---------- REGISTRO ----------
        [HttpGet]
        public IActionResult Registro() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registro(RegistroViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _db.Usuarios.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Ya existe una cuenta con este email");
                return View(model);
            }
            if (await _db.Usuarios.AnyAsync(u => u.NombreUsuario == model.NombreUsuario))
            {
                ModelState.AddModelError(nameof(model.NombreUsuario), "Ese nombre de usuario ya está ocupado");
                return View(model);
            }

            var usuario = new Usuario
            {
                NombreUsuario = model.NombreUsuario,
                Email = model.Email
            };
            // Encriptamos la contraseña antes de guardarla
            usuario.PasswordHash = _hasher.HashPassword(usuario, model.Password);

            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();

            await IniciarSesion(usuario);
            // Recién registrado -> "configura tu nueva cuenta"
            return RedirectToAction("Editar", "Perfil");
        }

        // ---------- LOGIN ----------
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (usuario == null ||
                _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, model.Password)
                    == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos");
                return View(model);
            }

            await IniciarSesion(usuario);

            // Si venías de una página protegida, te regresa ahí
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Perfil");
        }

        // ---------- LOGOUT ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // Crea la "credencial" (cookie) del usuario logueado
        private async Task IniciarSesion(Usuario usuario)
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