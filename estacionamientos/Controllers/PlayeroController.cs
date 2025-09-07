using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using estacionamientos.ViewModels;

namespace estacionamientos.Controllers
{
    public class PlayeroController : Controller
    {
        private readonly AppDbContext _context;
        public PlayeroController(AppDbContext context) => _context = context;

        // INDEX: muestra sólo playeros que trabajan en playas administradas por el dueño logueado
        public async Task<IActionResult> Index()
        {
            var dueId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var misPlyIds = await _context.Set<AdministraPlaya>()
                .Where(a => a.DueNU == dueId)
                .Select(a => a.PlyID)
                .ToListAsync();

            var trabajos = await _context.Set<TrabajaEn>()
                .Include(t => t.Playero)
                .Include(t => t.Playa)
                .Where(t => misPlyIds.Contains(t.PlyID))
                .AsNoTracking()
                .ToListAsync();

            var porPlayero = trabajos
                .GroupBy(t => t.Playero.UsuNU)
                .Select(g => new PlayeroIndexVM
                {
                    Playero = g.First().Playero,
                    Playas = g.Select(x => x.Playa).Distinct().ToList()
                })
                .OrderBy(vm => vm.Playero.UsuNyA)
                .ToList();

            return View(porPlayero);
        }

        public async Task<IActionResult> Details(int id)
        {
            var entity = await _context.Playeros.AsNoTracking()
                .FirstOrDefaultAsync(e => e.UsuNU == id);
            return entity is null ? NotFound() : View(entity);
        }

        // ===== CREATE (GET): carga sólo las playas del dueño =====
        public async Task<IActionResult> Create()
        {
            var dueId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var misPlayas = await _context.Set<AdministraPlaya>()
                .Where(a => a.DueNU == dueId)
                .Select(a => a.Playa)
                .OrderBy(p => p.PlyID)
                .Select(p => new
                {
                    p.PlyID,
                    Nombre = p.PlyNom + " (" + p.PlyCiu + ")"
                })
                .ToListAsync();

            ViewBag.Playas = new SelectList(misPlayas, "PlyID", "Nombre");

            return View(new PlayeroCreateVM
            {
                Playero = new Playero()
            });
        }

        // ===== CREATE (POST): crea Playero + TrabajaEn =====
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PlayeroCreateVM vm)
        {
            var dueId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var esMia = await _context.Set<AdministraPlaya>()
                .AnyAsync(a => a.DueNU == dueId && a.PlyID == vm.PlayaId);

            if (!esMia)
            {
                ModelState.AddModelError(nameof(vm.PlayaId), "No podés asignar a una playa que no administrás.");
            }

            if (!ModelState.IsValid)
            {
                var misPlayas = await _context.Set<AdministraPlaya>()
                    .Where(a => a.DueNU == dueId)
                    .Select(a => a.Playa)
                    .OrderBy(p => p.PlyID)
                    .Select(p => new
                    {
                        p.PlyID,
                        Nombre = "Playa #" + p.PlyID
                    })
                    .ToListAsync();

                ViewBag.Playas = new SelectList(misPlayas, "PlyID", "Nombre", vm.PlayaId);

                foreach (var kv in ModelState)
                {
                    var key = kv.Key;
                    var errs = string.Join(" | ", kv.Value.Errors.Select(e => e.ErrorMessage));
                    Console.WriteLine($"[ModelState] {key}: {errs}");
                }
                return View(vm);
            }

            _context.Playeros.Add(vm.Playero);
            await _context.SaveChangesAsync();

            var trabajo = new TrabajaEn
            {
                PlaNU = vm.Playero.UsuNU,
                PlyID = vm.PlayaId
            };

            _context.Set<TrabajaEn>().Add(trabajo);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _context.Playeros.FindAsync(id);
            return entity is null ? NotFound() : View(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Playero model)
        {
            if (id != model.UsuNU) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Playeros.FindAsync(id);
            if (entity == null) return NotFound();

            var relaciones = await _context.Set<TrabajaEn>()
                .Where(t => t.PlaNU == id)
                .ToListAsync();

            _context.Set<TrabajaEn>().RemoveRange(relaciones);
            _context.Playeros.Remove(entity);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ===== NUEVO: ver plazas de la playa del turno activo =====
        public async Task<IActionResult> Plazas()
        {
            var usuId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var turno = await _context.Turnos
                .Include(t => t.Playa)
                .FirstAsync(t => t.PlaNU == usuId && t.TurFyhFin == null);

            var plazas = await _context.Plazas
                .Where(p => p.PlyID == turno.PlyID)
                .OrderBy(p => p.PlzNum)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.PlyID = turno.PlyID;

            return View(plazas);
        }

        // Cambiar habilitación de una plaza =====
        [HttpPost]
        public async Task<IActionResult> ToggleHabilitada(int PlyID, int PlzNum)
        {
            var plaza = await _context.Plazas
                .FirstOrDefaultAsync(p => p.PlyID == PlyID && p.PlzNum == PlzNum);

            if (plaza == null)
            {
                return NotFound();
            }

            plaza.PlzHab = !plaza.PlzHab;
            _context.Update(plaza);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Plaza {plaza.PlzNum} {(plaza.PlzHab ? "habilitada" : "deshabilitada")}.";
            TempData["MensajeCss"] = plaza.PlzHab ? "success" : "danger";

            return RedirectToAction(nameof(Plazas));
        }
    }
}