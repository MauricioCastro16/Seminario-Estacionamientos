using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using System.Security.Claims;

namespace estacionamientos.Controllers
{
    public class TrabajaEnController : Controller
    {
        private readonly AppDbContext _ctx;
        public TrabajaEnController(AppDbContext ctx) => _ctx = ctx;

        private async Task LoadSelects(int? plySel = null, int? plaSel = null)
        {
            var dueId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // solo las playas administradas por el dueño logueado
            var playas = await _ctx.Set<AdministraPlaya>()
                .Where(a => a.DueNU == dueId)
                .Select(a => a.Playa)
                .OrderBy(p => p.PlyNom)
                .Select(p => new
                {
                    p.PlyID,
                    Nombre = p.PlyNom + " (" + p.PlyCiu + ")"
                })
                .ToListAsync();

            var playeros = await _ctx.Playeros.AsNoTracking()
                .OrderBy(p => p.UsuNyA)
                .Select(p => new { p.UsuNU, p.UsuNyA })
                .ToListAsync();

            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", plySel);
            ViewBag.PlaNU = new SelectList(playeros, "UsuNU", "UsuNyA", plaSel);
        }

        // GET: /TrabajaEn
        public async Task<IActionResult> Index()
        {
            var q = _ctx.Trabajos
                .Include(t => t.Playa)
                .Include(t => t.Playero)
                .Where(t => t.FechaFin == null) // <-- ahora por fecha
                .AsNoTracking();

            return View(await q.ToListAsync());
        }


        // GET: /TrabajaEn/NuevaAsignacion?plaNU=38
        [HttpGet]
        public async Task<IActionResult> NuevaAsignacion(int? plaNU = null, string? returnUrl = null)
        {
            await LoadSelects(null, plaNU);
            ViewBag.ReturnUrl = returnUrl;
            return View("Create", new TrabajaEn { PlaNU = plaNU ?? 0 });
        }

        // POST: /TrabajaEn/NuevaAsignacion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NuevaAsignacionPost(TrabajaEn model, string? returnUrl = null)
        {
            // evitar duplicados exactos (misma clave compuesta)
            var existente = await _ctx.Trabajos
                .FirstOrDefaultAsync(x => x.PlyID == model.PlyID && x.PlaNU == model.PlaNU);

            if (existente is not null && existente.FechaFin == null)
                ModelState.AddModelError(string.Empty, "Ese playero ya está asignado a esa playa.");

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.PlaNU);
                ViewBag.ReturnUrl = returnUrl;
                return View("Create", model);
            }

            if (existente is null)
            {
                model.TrabEnActual = true;       // compatibilidad
                model.FechaInicio = DateTime.UtcNow;
                model.FechaFin = null;
                _ctx.Trabajos.Add(model);
            }
            else
            {
                // reactivar: nuevo período
                existente.TrabEnActual = true;   // compatibilidad
                existente.FechaInicio = DateTime.UtcNow;
                existente.FechaFin = null;
                _ctx.Update(existente);
            }

            await _ctx.SaveChangesAsync();
            return RedirectToAction("Index", "Playero");
        }



        // GET: /TrabajaEn/Delete?plyID=5&plaNU=38
        [HttpGet]
        public async Task<IActionResult> Delete(int plyID, int plaNU)
        {
            var item = await _ctx.Trabajos
                .Include(t => t.Playa)
                .Include(t => t.Playero)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.PlyID == plyID && t.PlaNU == plaNU);

            return item is null ? NotFound() : View(item);
        }

        // POST: /TrabajaEn/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int plaNU)
        {
            var item = await _ctx.Trabajos.FindAsync(plyID, plaNU);
            if (item is null) return NotFound();

            item.TrabEnActual = false;      // compatibilidad
            item.FechaFin = DateTime.UtcNow;   // marcar histórico
            await _ctx.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDirect(int plyID, int plaNU)
        {
            // ¿existen turnos que referencian esta relación?
            var tieneTurnos = await _ctx.Set<Turno>()
                .AnyAsync(t => t.PlyID == plyID && t.PlaNU == plaNU);

            if (tieneTurnos)
            {
                TempData["Error"] = "No se puede desasignar porque existen turnos vinculados.";
                return RedirectToAction("Index", "Playero");
            }

            var item = await _ctx.Trabajos.FindAsync(plyID, plaNU);
            if (item is null) return NotFound();

            _ctx.Trabajos.Remove(item);
            await _ctx.SaveChangesAsync();

            TempData["Ok"] = "Asignación eliminada correctamente.";
            return RedirectToAction("Index", "Playero");
        }


    }
}