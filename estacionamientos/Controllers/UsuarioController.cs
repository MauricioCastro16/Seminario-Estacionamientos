using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.Models.ViewModels;

namespace estacionamientos.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly AppDbContext _context;
        public UsuarioController(AppDbContext context) => _context = context;

        // GET: /Usuario
        public async Task<IActionResult> Index()
        {
            var lista = await _context.Usuarios.AsNoTracking().ToListAsync();
            return View(lista);
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
                // ⚠️ En producción, guardar HASH en lugar de texto plano
                UsuPswd = vm.UsuPswd,
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
    }
}

