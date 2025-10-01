using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.Models.ViewModels;
using BCrypt.Net;
// using Microsoft.AspNetCore.Authorization;

namespace estacionamientos.Controllers
{
    public class UsuarioController : Controller
    {
        // [Authorize(Roles = "Administrador")]
        private readonly AppDbContext _context;
        public UsuarioController(AppDbContext context) => _context = context;

        // GET: /Usuario
        public async Task<IActionResult> Index()
        {
            var vm = new UsuariosIndexVM
            {
                Duenios = await _context.Duenios.AsNoTracking().OrderBy(d => d.UsuNyA).ToListAsync(),
                Conductores = await _context.Conductores.AsNoTracking().OrderBy(c => c.UsuNyA).ToListAsync(),
                Administradores = await _context.Administradores.AsNoTracking().OrderBy(a => a.UsuNyA).ToListAsync(),
                Playeros = await _context.Playeros.AsNoTracking().OrderBy(p => p.UsuNyA).ToListAsync()
            };

            return View(vm);
        }

        // GET: /Usuario/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var usuario = await _context.Usuarios.AsNoTracking()
                               .FirstOrDefaultAsync(u => u.UsuNU == id);
            return usuario is null ? NotFound() : View(usuario);
        }

        // GET: /Usuario/Create
        public IActionResult Create() => View();

        // POST: /Usuario/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario model)
        {
            if (!ModelState.IsValid) return View(model);

            _context.Add(model);
            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, $"Error guardando: {ex.Message}");
                return View(model);
            }
        }

        // GET: /Usuario/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            return usuario is null ? NotFound() : View(usuario);
        }

        // POST: /Usuario/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Usuario model)
        {
            if (id != model.UsuNU) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            _context.Entry(model).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.Usuarios.AnyAsync(u => u.UsuNU == id);
                if (!exists) return NotFound();
                throw;
            }
        }

        // GET: /Usuario/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var usuario = await _context.Usuarios.AsNoTracking()
                               .FirstOrDefaultAsync(u => u.UsuNU == id);
            return usuario is null ? NotFound() : View(usuario);
        }

        // POST: /Usuario/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario is null) return NotFound();

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ======================
        // ====== DUEÑOS ========
        // ======================

        // GET: /Usuario/CreateDuenio
        public IActionResult CreateDuenio() => View(new CreateDuenioVM());

        // POST: /Usuario/CreateDuenio
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDuenio(CreateDuenioVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // Validación de unicidad de email (evita excepción por índice único)
            var emailEnUso = await _context.Usuarios
                .AsNoTracking()
                .AnyAsync(u => u.UsuEmail == vm.UsuEmail);
            if (emailEnUso)
            {
                ModelState.AddModelError(nameof(vm.UsuEmail), "El email ya está en uso.");
                return View(vm);
            }

            // Mapear VM -> entidad derivada (Duenio : Usuario)
            var duenio = new Duenio
            {
                UsuNyA = vm.UsuNyA,
                UsuEmail = vm.UsuEmail,
                // 🔐 Contraseña hasheada con BCrypt
                UsuPswd = BCrypt.Net.BCrypt.HashPassword(vm.UsuPswd),
                UsuNumTel = vm.UsuNumTel,
                DueCuit = vm.DueCuit
            };

            _context.Duenios.Add(duenio);
            try
            {
                await _context.SaveChangesAsync();
                TempData["Msg"] = "Dueño creado correctamente.";
                return RedirectToAction(nameof(Index)); // índice de usuarios (o redirigí a Duenio/Index si preferís)
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, $"Error guardando: {ex.InnerException?.Message ?? ex.Message}");
                return View(vm);
            }
        }

        public async Task<IActionResult> EditDuenio(int id)
        {
            var duenio = await _context.Duenios.FindAsync(id);
            if (duenio is null) return NotFound();

            return View("Duenio/Edit", duenio); // busca en /Views/Usuario/Duenio/Edit.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDuenio(int id, Duenio model)
        {
            if (id != model.UsuNU) return BadRequest();
            if (!ModelState.IsValid) return View("Duenio/Edit", model);

            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DetailsDuenio(int id)
        {
            var duenio = await _context.Duenios.AsNoTracking().FirstOrDefaultAsync(d => d.UsuNU == id);
            if (duenio is null) return NotFound();

            return View("Duenio/Details", duenio); // /Views/Usuario/Duenio/Details.cshtml
        }

        public async Task<IActionResult> DeleteDuenio(int id)
        {
            var duenio = await _context.Duenios.AsNoTracking().FirstOrDefaultAsync(d => d.UsuNU == id);
            if (duenio is null) return NotFound();

            return View("Duenio/Delete", duenio); // /Views/Usuario/Duenio/Delete.cshtml
        }

        [HttpPost, ActionName("DeleteDuenio")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDuenioConfirmed(int id)
        {
            var duenio = await _context.Duenios.FindAsync(id);
            if (duenio is null) return NotFound();

            _context.Duenios.Remove(duenio);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ==========================
        // ====== CONDUCTORES =======
        // ==========================

        // GET: /Usuario/CreateConductor
        public IActionResult CreateConductor() => View(new CreateConductorVM());

        // POST: /Usuario/CreateConductor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateConductor(CreateConductorVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // Email único (evita romper índice único)
            var emailUsado = await _context.Usuarios
                .AsNoTracking()
                .AnyAsync(u => u.UsuEmail == vm.UsuEmail);
            if (emailUsado)
            {
                ModelState.AddModelError(nameof(vm.UsuEmail), "El email ya está en uso.");
                return View(vm);
            }

            // Map VM -> entidad derivada (Conductor : Usuario)
            var entity = new Conductor
            {
                UsuNyA = vm.UsuNyA,
                UsuEmail = vm.UsuEmail,
                UsuPswd = BCrypt.Net.BCrypt.HashPassword(vm.UsuPswd), // 🔐 Contraseña hasheada
                UsuNumTel = vm.UsuNumTel
            };

            _context.Conductores.Add(entity);
            try
            {
                await _context.SaveChangesAsync();
                TempData["Msg"] = "Conductor creado correctamente.";
                return RedirectToAction(nameof(Index)); // o a Index del Conductor si preferís
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, $"Error guardando: {ex.InnerException?.Message ?? ex.Message}");
                return View(vm);
            }
        }

        public async Task<IActionResult> EditConductor(int id)
        {
            var conductor = await _context.Conductores.FindAsync(id);
            if (conductor is null) return NotFound();

            return View("Conductor/Edit", conductor); // /Views/Usuario/Conductor/Edit.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditConductor(int id, Conductor model)
        {
            if (id != model.UsuNU) return BadRequest();
            if (!ModelState.IsValid) return View("Conductor/Edit", model);

            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DetailsConductor(int id)
        {
            var conductor = await _context.Conductores.AsNoTracking().FirstOrDefaultAsync(c => c.UsuNU == id);
            if (conductor is null) return NotFound();

            return View("Conductor/Details", conductor); // /Views/Usuario/Conductor/Details.cshtml
        }

        public async Task<IActionResult> DeleteConductor(int id)
        {
            var conductor = await _context.Conductores.AsNoTracking().FirstOrDefaultAsync(c => c.UsuNU == id);
            if (conductor is null) return NotFound();

            return View("Conductor/Delete", conductor); // /Views/Usuario/Conductor/Delete.cshtml
        }

        [HttpPost, ActionName("DeleteConductor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConductorConfirmed(int id)
        {
            var conductor = await _context.Conductores.FindAsync(id);
            if (conductor is null) return NotFound();

            _context.Conductores.Remove(conductor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // ==========================
        // ===== ADMINISTRADORES ====
        // ==========================
        // GET: /Usuario/CreateAdministrador
        public IActionResult CreateAdministrador() => View(new CreateAdministradorVM());

        // POST: /Usuario/CreateAdministrador
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdministrador(CreateAdministradorVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var emailUsado = await _context.Usuarios.AnyAsync(u => u.UsuEmail == vm.UsuEmail);
            if (emailUsado)
            {
                ModelState.AddModelError(nameof(vm.UsuEmail), "El email ya está en uso.");
                return View(vm);
            }

            var entity = new Administrador
            {
                UsuNyA = vm.UsuNyA,
                UsuEmail = vm.UsuEmail,
                UsuPswd = BCrypt.Net.BCrypt.HashPassword(vm.UsuPswd), // 🔐 Contraseña hasheada
                UsuNumTel = vm.UsuNumTel
            };

            _context.Administradores.Add(entity);
            await _context.SaveChangesAsync();
            TempData["Msg"] = "Administrador creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET/POST: EditAdministrador, DetailsAdministrador, DeleteAdministrador
        public async Task<IActionResult> EditAdministrador(int id)
        {
            var administrador = await _context.Administradores.FindAsync(id);
            if (administrador is null) return NotFound();

            return View("Administrador/Edit", administrador);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAdministrador(int id, Administrador model)
        {
            if (id != model.UsuNU) return BadRequest();
            if (!ModelState.IsValid) return View("Administrador/Edit", model);

            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DetailsAdministrador(int id)
        {
            var administrador = await _context.Administradores.AsNoTracking().FirstOrDefaultAsync(a => a.UsuNU == id);
            if (administrador is null) return NotFound();

            return View("Administrador/Details", administrador);
        }

        public async Task<IActionResult> DeleteAdministrador(int id)
        {
            var administrador = await _context.Administradores.AsNoTracking().FirstOrDefaultAsync(a => a.UsuNU == id);
            if (administrador is null) return NotFound();

            return View("Administrador/Delete", administrador);
        }

        [HttpPost, ActionName("DeleteAdministrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAdministradorConfirmed(int id)
        {
            var administrador = await _context.Administradores.FindAsync(id);
            if (administrador is null) return NotFound();

            _context.Administradores.Remove(administrador);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ==========================
        // ======== PLAYEROS ========
        // ==========================

        // GET: /Usuario/CreatePlayero
        public IActionResult CreatePlayero() => View(new CreatePlayeroVM());

        // POST: /Usuario/CreatePlayero
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePlayero(CreatePlayeroVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var emailUsado = await _context.Usuarios.AnyAsync(u => u.UsuEmail == vm.UsuEmail);
            if (emailUsado)
            {
                ModelState.AddModelError(nameof(vm.UsuEmail), "El email ya está en uso.");
                return View(vm);
            }

            var entity = new Playero
            {
                UsuNyA = vm.UsuNyA,
                UsuEmail = vm.UsuEmail,
                UsuPswd = BCrypt.Net.BCrypt.HashPassword(vm.UsuPswd), // 🔐 Contraseña hasheada
                UsuNumTel = vm.UsuNumTel
            };

            _context.Playeros.Add(entity);
            await _context.SaveChangesAsync();
            TempData["Msg"] = "Playero creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET/POST: EditPlayero, DetailsPlayero, DeletePlayero
        public async Task<IActionResult> EditPlayero(int id)
        {
            var playero = await _context.Playeros.FindAsync(id);
            if (playero is null) return NotFound();

            return View("Playero/Edit", playero);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPlayero(int id, Playero model)
        {
            if (id != model.UsuNU) return BadRequest();
            if (!ModelState.IsValid) return View("Playero/Edit", model);

            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> DetailsPlayero(int id)
        {
            var playero = await _context.Playeros.AsNoTracking().FirstOrDefaultAsync(p => p.UsuNU == id);
            if (playero is null) return NotFound();

            return View("Playero/Details", playero);
        }
        public async Task<IActionResult> DeletePlayero(int id)
        {
            var playero = await _context.Playeros.AsNoTracking().FirstOrDefaultAsync(p => p.UsuNU == id);
            if (playero is null) return NotFound();

            return View("Playero/Delete", playero);
        }
        [HttpPost, ActionName("DeletePlayero")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePlayeroConfirmed(int id)
        {
            var playero = await _context.Playeros.FindAsync(id);
            if (playero is null) return NotFound();

            _context.Playeros.Remove(playero);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}

