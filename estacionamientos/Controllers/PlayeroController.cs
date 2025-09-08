using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.ViewModels;

namespace estacionamientos.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Duenio")]
    public class PlayeroController : Controller
    {
        private readonly AppDbContext _context;
        public PlayeroController(AppDbContext context) => _context = context;

        // ------------------------------------------------------------
        // VMs locales para que el controlador quede autocontenido
        // ------------------------------------------------------------
        public sealed class PlayeroAssignVM
        {
            public int PlaNU { get; set; }               // UsuNU del playero
            public int PlayaId { get; set; }             // PlyID seleccionado
            public string? PlayeroNombre { get; set; }   // sólo display
        }

        // ------------------------------------------------------------
        // HELPERS
        // ------------------------------------------------------------
        private int GetCurrentOwnerId()
            => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task<List<int>> PlyIdsDelDuenioAsync(int dueId)
            => await _context.AdministraPlayas
                .Where(a => a.DueNU == dueId)
                .Select(a => a.PlyID)
                .ToListAsync();

        private async Task<SelectList> SelectListPlayasDelDuenioAsync(int dueId, int? selected = null)
        {
            var misPlayas = await _context.AdministraPlayas
                .Where(a => a.DueNU == dueId)
                .Select(a => a.Playa)
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new
                {
                    p.PlyID,
                    Nombre = string.IsNullOrWhiteSpace(p.PlyNom)
                        ? $"{p.PlyCiu} - {p.PlyDir}"
                        : p.PlyNom
                })
                .ToListAsync();

            return new SelectList(misPlayas, "PlyID", "Nombre", selected);
        }

        // ------------------------------------------------------------
        // INDEX: sólo vínculos ACTIVOS en playas del dueño logueado
        // ------------------------------------------------------------
        public async Task<IActionResult> Index()
        {
            var dueId = GetCurrentOwnerId();
            var misPlyIds = await PlyIdsDelDuenioAsync(dueId);

            var trabajosActivos = await _context.Trabajos
                .Include(t => t.Playero)
                .Include(t => t.Playa)
                .Where(t => misPlyIds.Contains(t.PlyID) && t.TrabEnActual)
                .AsNoTracking()
                .ToListAsync();

            var porPlayero = trabajosActivos
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

        // ------------------------------------------------------------
        // DETAILS (simple)
        // ------------------------------------------------------------
        public async Task<IActionResult> Details(int id)
        {
            var entity = await _context.Playeros.AsNoTracking()
                .FirstOrDefaultAsync(e => e.UsuNU == id);
            return entity is null ? NotFound() : View(entity);
        }

        // ------------------------------------------------------------
        // CREATE (GET): sólo playas del dueño
        // ------------------------------------------------------------
        public async Task<IActionResult> Create()
        {
            var dueId = GetCurrentOwnerId();
            ViewBag.Playas = await SelectListPlayasDelDuenioAsync(dueId);
            return View(new PlayeroCreateVM
            {
                Playero = new Playero()
            });
        }

        // ------------------------------------------------------------
        // CREATE (POST): crea Playero + vínculo ACTIVO
        // ------------------------------------------------------------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PlayeroCreateVM vm)
        {
            var dueId = GetCurrentOwnerId();

            // Guard 1: playa debe ser del dueño
            var esMia = await _context.AdministraPlayas
                .AnyAsync(a => a.DueNU == dueId && a.PlyID == vm.PlayaId);
            if (!esMia)
                ModelState.AddModelError(nameof(vm.PlayaId), "No podés asignar a una playa que no administrás.");

            if (!ModelState.IsValid)
            {
                ViewBag.Playas = await SelectListPlayasDelDuenioAsync(dueId, vm.PlayaId);
                return View(vm);
            }

            // Alta Playero
            _context.Playeros.Add(vm.Playero);
            await _context.SaveChangesAsync();

            // Vínculo activo
            var trabajo = new TrabajaEn
            {
                PlaNU = vm.Playero.UsuNU,
                PlyID = vm.PlayaId,
                TrabEnActual = true
            };
            _context.Trabajos.Add(trabajo);
            await _context.SaveChangesAsync();

            TempData["Msg"] = "Playero creado y asignado.";
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------
        // EDIT (datos básicos del playero)
        // ------------------------------------------------------------
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _context.Playeros.FindAsync(id);
            return entity is null ? NotFound() : View(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Playero playero)
        {
            if (id != playero.UsuNU) return BadRequest();

            if (!ModelState.IsValid) return View(playero);

            try
            {
                _context.Update(playero);
                await _context.SaveChangesAsync();
                TempData["Msg"] = "Datos del playero actualizados.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Playeros.AnyAsync(e => e.UsuNU == id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------
        // ASSIGN (GET): combo con playas del dueño
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Assign(int id) // id = UsuNU del playero
        {
            var playero = await _context.Playeros.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UsuNU == id);
            if (playero is null) return NotFound();

            var dueId = GetCurrentOwnerId();
            ViewBag.Playas = await SelectListPlayasDelDuenioAsync(dueId);

            return View(new PlayeroAssignVM
            {
                PlaNU = playero.UsuNU,
                PlayeroNombre = playero.UsuNyA
            });
        }

        // ------------------------------------------------------------
        // ASSIGN (POST): crea o REACTIVA vínculo
        // ------------------------------------------------------------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(PlayeroAssignVM vm)
        {
            var dueId = GetCurrentOwnerId();

            // Guard 1: playa debe ser del dueño
            var esMia = await _context.AdministraPlayas
                .AnyAsync(a => a.DueNU == dueId && a.PlyID == vm.PlayaId);
            if (!esMia)
                ModelState.AddModelError(nameof(vm.PlayaId), "No podés asignar a una playa que no administrás.");

            // Vínculo existente (para reactivar si es histórico)
            var existente = await _context.Trabajos
                .FirstOrDefaultAsync(t => t.PlaNU == vm.PlaNU && t.PlyID == vm.PlayaId);

            if (existente is not null && existente.TrabEnActual)
                ModelState.AddModelError(nameof(vm.PlayaId), "El playero ya está vinculado a esa playa.");

            if (!ModelState.IsValid)
            {
                ViewBag.Playas = await SelectListPlayasDelDuenioAsync(dueId, vm.PlayaId);
                var p = await _context.Playeros.AsNoTracking().FirstOrDefaultAsync(x => x.UsuNU == vm.PlaNU);
                vm.PlayeroNombre = p?.UsuNyA;
                return View(vm);
            }

            if (existente is null)
            {
                _context.Trabajos.Add(new TrabajaEn
                {
                    PlaNU = vm.PlaNU,
                    PlyID = vm.PlayaId,
                    TrabEnActual = true
                });
            }
            else
            {
                existente.TrabEnActual = true; // reactivar
                _context.Update(existente);
            }

            await _context.SaveChangesAsync();
            TempData["Msg"] = "Playero vinculado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------
        // UNASSIGN: marcar histórico (no borrar)
        // ------------------------------------------------------------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Unassign(int plaNU, int plyID)
        {
            var dueId = GetCurrentOwnerId();

            // asegurar propiedad
            var esMia = await _context.AdministraPlayas
                .AnyAsync(a => a.DueNU == dueId && a.PlyID == plyID);
            if (!esMia) return Forbid();

            var rel = await _context.Trabajos
                .FirstOrDefaultAsync(t => t.PlaNU == plaNU && t.PlyID == plyID);
            if (rel is null) return NotFound();

            rel.TrabEnActual = false; // histórico
            await _context.SaveChangesAsync();

            TempData["Msg"] = "Vinculación marcada como histórica.";
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------
        // DELETE: ocultar al playero para este dueño (marcar TODO histórico)
        // ------------------------------------------------------------
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Playeros.AsNoTracking()
                .FirstOrDefaultAsync(e => e.UsuNU == id);
            return entity is null ? NotFound() : View(entity);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dueId = GetCurrentOwnerId();
            var misPlyIds = await PlyIdsDelDuenioAsync(dueId);

            // Marcar como histórico todas las relaciones activas del playero en MIS playas
            var rels = await _context.Trabajos
                .Where(t => t.PlaNU == id && misPlyIds.Contains(t.PlyID) && t.TrabEnActual)
                .ToListAsync();

            foreach (var r in rels)
                r.TrabEnActual = false;

            await _context.SaveChangesAsync();

            // Nota: NO eliminamos al Playero (conservamos identidad, auditoría, etc.)
            TempData["Msg"] = "El playero ya no aparece en tus listados. Se conservó el historial.";
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleHabilitada(int PlyID, int PlzNum)
        {
            var plaza = await _context.Plazas
                .FirstOrDefaultAsync(p => p.PlyID == PlyID && p.PlzNum == PlzNum);

            if (plaza == null) return NotFound();

            plaza.PlzHab = !plaza.PlzHab;
            _context.Update(plaza);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Plaza {plaza.PlzNum} {(plaza.PlzHab ? "habilitada" : "deshabilitada")}.";
            TempData["MensajeCss"] = plaza.PlzHab ? "success" : "danger";

            return RedirectToAction(nameof(Plazas));
        }
    }
}
