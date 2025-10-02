using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.Models.ViewModels.Auth;
using BCrypt.Net;

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
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1) Buscar usuario por email
            var user = await _ctx.Usuarios
                .FirstOrDefaultAsync(u => u.UsuEmail == model.EmailOrUsername || u.UsuNomUsu == model.EmailOrUsername);

            // 2) Verificar contraseña
            var passwordOk = false;
            if (user is not null)
            {
                try
                {
                    // 🔐 Verificar contraseña hasheada con BCrypt
                    passwordOk = BCrypt.Net.BCrypt.Verify(model.Password, user.UsuPswd);
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    // Si hay un error de salt, intentar con la contraseña en texto plano (solo para migración)
                    // NOTA: Esto es temporal para usuarios con hashes incompatibles
                    passwordOk = model.Password == user.UsuPswd;
                    
                    // Si la contraseña coincide, actualizar el hash con la versión correcta
                    if (passwordOk)
                    {
                        user.UsuPswd = BCrypt.Net.BCrypt.HashPassword(model.Password);
                        await _ctx.SaveChangesAsync();
                    }
                }
            }

            if (user is null || !passwordOk)
            {
                ModelState.AddModelError(string.Empty, "Email o contraseña inválidos.");
                return View(model);
            }

            // 3) ¿Qué rol tiene?
            var esAdmin = await _ctx.Administradores
                .AnyAsync(a => a.UsuNU == user.UsuNU);
            var esPlayero = await _ctx.Playeros
                .AnyAsync(p => p.UsuNU == user.UsuNU);
            var esConductor = await _ctx.Conductores
                .AnyAsync(c => c.UsuNU == user.UsuNU);
            var esDuenio = await _ctx.Duenios
                .AnyAsync(d => d.UsuNU == user.UsuNU);

            // 4) Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UsuNU.ToString()),
                new Claim(ClaimTypes.Name, user.UsuNyA),
                new Claim(ClaimTypes.Email, user.UsuEmail)
            };

            if (esAdmin)
                claims.Add(new Claim(ClaimTypes.Role, "Administrador"));
            else if (esPlayero)
                claims.Add(new Claim(ClaimTypes.Role, "Playero"));
            else if (esConductor)
                claims.Add(new Claim(ClaimTypes.Role, "Conductor"));
            else if (esDuenio)
                claims.Add(new Claim(ClaimTypes.Role, "Duenio"));

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

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Validación adicional del checkbox
            if (!model.AcceptTerms)
            {
                ModelState.AddModelError(nameof(model.AcceptTerms), "Debe aceptar los términos y condiciones.");
            }

            if (!ModelState.IsValid) return View(model);

            // Verificar si el email ya existe
            var emailExists = await _ctx.Usuarios.AsNoTracking()
                .AnyAsync(u => u.UsuEmail == model.UsuEmail);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.UsuEmail), "Este correo electrónico ya está registrado.");
                return View(model);
            }

            // Verificar si el nombre de usuario ya existe
            var usernameExists = await _ctx.Usuarios.AsNoTracking()
                .AnyAsync(u => u.UsuNomUsu == model.UsuNomUsu);
            if (usernameExists)
            {
                ModelState.AddModelError(nameof(model.UsuNomUsu), "Este nombre de usuario ya está en uso.");
                return View(model);
            }

            // Calcular el siguiente UsuNU disponible dinámicamente:
            int nextUsuNu = Math.Max(9, (await _ctx.Usuarios.MaxAsync(u => u.UsuNU)) + 1);

            // Verificar que no haya colisión con el valor de UsuNU
            while (await _ctx.Usuarios.AnyAsync(u => u.UsuNU == nextUsuNu))
            {
                nextUsuNu++;
            }

            // Crear el conductor con UsuNU calculado
            var conductor = new Conductor
            {
                UsuNU = nextUsuNu,
                UsuNyA = model.UsuNyA,
                UsuNomUsu = model.UsuNomUsu,
                UsuEmail = model.UsuEmail,
                UsuPswd = BCrypt.Net.BCrypt.HashPassword(model.UsuPswd), // 🔐 Contraseña hasheada
                UsuNumTel = model.UsuNumTel
            };

            _ctx.Conductores.Add(conductor);
            await _ctx.SaveChangesAsync();

            // Redirigir al login con mensaje de éxito
            TempData["SuccessMessage"] = "¡Registro exitoso! Ya puedes iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }

        // Acceso denegado
        [HttpGet]
        public IActionResult Denied() => View();
    }
}
