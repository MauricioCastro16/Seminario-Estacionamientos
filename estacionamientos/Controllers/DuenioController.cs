using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class DuenioController : Controller
    {
        private readonly AppDbContext _context;
        public DuenioController(AppDbContext context) => _context = context;

        // GET: /Duenio
        public async Task<IActionResult> Index()
        {
            var lista = await _context.Duenios.AsNoTracking().ToListAsync();
            return View(lista);
        }

        // GET: /Duenio/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var duenio = await _context.Duenios.AsNoTracking()
                            .FirstOrDefaultAsync(d => d.UsuNU == id);
            return duenio is null ? NotFound() : View(duenio);
        }

        // GET: /Duenio/Create
        public IActionResult Create() => View(new Duenio());

        // POST: /Duenio/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Duenio model)
        {
            // model incluye campos de Usuario (NyA, Email, Pswd, NumTel) + DueCuit
            if (!ModelState.IsValid) return View(model);

            _context.Duenios.Add(model); // EF inserta en Usuario y luego en Duenio (TPT)
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

        // GET: /Duenio/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var duenio = await _context.Duenios.FindAsync(id);
            return duenio is null ? NotFound() : View(duenio);
        }

        // POST: /Duenio/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Duenio model)
        {
            if (id != model.UsuNU) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            // Marcamos la entidad derivada como modificada (incluye propiedades base)
            _context.Entry(model).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.Duenios.AnyAsync(d => d.UsuNU == id);
                if (!exists) return NotFound();
                throw;
            }
        }

        // GET: /Duenio/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var duenio = await _context.Duenios.AsNoTracking()
                            .FirstOrDefaultAsync(d => d.UsuNU == id);
            return duenio is null ? NotFound() : View(duenio);
        }

        // POST: /Duenio/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var duenio = await _context.Duenios.FindAsync(id);
            if (duenio is null) return NotFound();

            _context.Duenios.Remove(duenio); // EF borrará fila de Duenio; si querés cascada hacia Usuario, avisame
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
