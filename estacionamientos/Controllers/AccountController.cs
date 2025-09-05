using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.Models.ViewModels.Auth;

namespace estacionamientos.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly AppDbContext _ctx;
        public AccountController(AppDbContext ctx) => _ctx = ctx;

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
            => View(new LoginViewModel { ReturnUrl = returnUrl });

        // POST: /Account/Login
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1) Buscar usuario por email
            var user = await _ctx.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UsuEmail == model.Email);

            // 2) Verificar contraseña
            var passwordOk = false;
            if (user is not null)
            {
                // ✅ Plano (como lo tenés hoy)
                passwordOk = user.UsuPswd == model.Password;

                // 🔐 Recomendado (cuando migres a hash):
                // passwordOk = BCrypt.Net.BCrypt.Verify(model.Password, user.UsuPswd);
            }

            if (user is null || !passwordOk)
            {
                ModelState.AddModelError(string.Empty, "Email o contraseña inválidos.");
                return View(model);
            }

            // 3) ¿Qué rol tiene?
            var esAdmin = await _ctx.Administradores.AsNoTracking()
                .AnyAsync(a => a.UsuNU == user.UsuNU);
            var esPlayero = await _ctx.Playeros.AsNoTracking()
                .AnyAsync(p => p.UsuNU == user.UsuNU);
            var esConductor = await _ctx.Conductores.AsNoTracking()
                .AnyAsync(c => c.UsuNU == user.UsuNU);
            var esDuenio = await _ctx.Duenios.AsNoTracking()
                .AnyAsync(d => d.UsuNU == user.UsuNU);

            // 4) Claims (lo mínimo: ID, nombre, email, rol si aplica)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UsuNU.ToString()),
                new Claim(ClaimTypes.Name, user.UsuNyA),
                new Claim(ClaimTypes.Email, user.UsuEmail)
            };

            if (esAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Administrador"));
            } 
            else if (esPlayero)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Playero"));
            } 
            else if (esConductor)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Conductor"));
            } 
            else if (esDuenio)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Duenio"));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // 5) SignIn
            var authProps = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

            // 6) Redirigir
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }


        // (Opcional) Acceso denegado
        [HttpGet]
        public IActionResult Denied() => View();
    }
}
