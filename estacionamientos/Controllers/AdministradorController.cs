using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class AdministradorController : Controller
    {
        private readonly AppDbContext _context;
        public AdministradorController(AppDbContext context) => _context = context;

        // GET: /Administrador
        public async Task<IActionResult> Index()
        {
            var lista = await _context.Administradores.AsNoTracking().ToListAsync();
            return View(lista);
        }

        // GET: /Administrador/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var admin = await _context.Administradores.AsNoTracking()
                            .FirstOrDefaultAsync(a => a.UsuNU == id);
            return admin is null ? NotFound() : View(admin);
        }

        // GET: /Administrador/Create
        public IActionResult Create() => View(new Administrador());

        // POST: /Administrador/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Administrador model)
        {
            if (!ModelState.IsValid) return View(model);

            _context.Administradores.Add(model); // Inserta en Usuario y luego en Administrador (TPT)
            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, $"Error guardando: {ex.InnerException?.Message ?? ex.Message}");
                return View(model);
            }
        }

        // GET: /Administrador/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var admin = await _context.Administradores.FindAsync(id);
            return admin is null ? NotFound() : View(admin);
        }

        // POST: /Administrador/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Administrador model)
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
                var exists = await _context.Administradores.AnyAsync(a => a.UsuNU == id);
                if (!exists) return NotFound();
                throw;
            }
        }

        // GET: /Administrador/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var admin = await _context.Administradores.AsNoTracking()
                            .FirstOrDefaultAsync(a => a.UsuNU == id);
            return admin is null ? NotFound() : View(admin);
        }

        // POST: /Administrador/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var admin = await _context.Administradores.FindAsync(id);
            if (admin is null) return NotFound();

            _context.Administradores.Remove(admin);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
