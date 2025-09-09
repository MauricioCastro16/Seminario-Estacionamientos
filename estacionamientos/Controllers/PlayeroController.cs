using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.ViewModels;

namespace estacionamientos.Controllers
{
    // Permitir que entren tanto Duenio como Playero
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Duenio,Playero")]
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
        // INDEX: sólo dueños
        // ------------------------------------------------------------
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Index()
        {
            var dueId = GetCurrentOwnerId();
            var misPlyIds = await PlyIdsDelDuenioAsync(dueId);

            // INDEX: solo vínculos vigentes en playas del dueño
            var trabajosActivos = await _context.Trabajos
                .Include(t => t.Playero)
                .Include(t => t.Playa)
                .Where(t => misPlyIds.Contains(t.PlyID) && t.FechaFin == null) // <-- por fecha
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
        // DETAILS: sólo dueños
        // ------------------------------------------------------------
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Details(int id)
        {
            var entity = await _context.Playeros.AsNoTracking()
                .FirstOrDefaultAsync(e => e.UsuNU == id);
            return entity is null ? NotFound() : View(entity);
        }

        // ------------------------------------------------------------
        // CREATE: sólo dueños
        // ------------------------------------------------------------
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Create()
        {
            var dueId = GetCurrentOwnerId();
            ViewBag.Playas = await SelectListPlayasDelDuenioAsync(dueId);
            return View(new PlayeroCreateVM
            {
                Playero = new Playero()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Create(PlayeroCreateVM vm)
        {
            var dueId = GetCurrentOwnerId();

            var esMia = await _context.AdministraPlayas
                .AnyAsync(a => a.DueNU == dueId && a.PlyID == vm.PlayaId);
            if (!esMia)
                ModelState.AddModelError(nameof(vm.PlayaId), "No podés asignar a una playa que no administrás.");

            if (!ModelState.IsValid)
            {
                ViewBag.Playas = await SelectListPlayasDelDuenioAsync(dueId, vm.PlayaId);
                return View(vm);
            }

            _context.Playeros.Add(vm.Playero);
            await _context.SaveChangesAsync();

            // CREATE (POST): vínculo inicial
            var trabajo = new TrabajaEn
            {
                PlaNU = vm.Playero.UsuNU,
                PlyID = vm.PlayaId,
                TrabEnActual = true,          // compatibilidad
                FechaInicio = DateTime.Now,
                FechaFin = null
            };
            _context.Trabajos.Add(trabajo);

            await _context.SaveChangesAsync();

            TempData["Msg"] = "Playero creado y asignado.";
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------
        // EDIT: sólo dueños
        // ------------------------------------------------------------
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _context.Playeros.FindAsync(id);
            return entity is null ? NotFound() : View(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Duenio")]
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
        // ASSIGN: sólo dueños (GET)
        // ------------------------------------------------------------
        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Assign(int id)
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
        // ASSIGN: crea o REACTIVA vínculo con historial (POST)
        // ------------------------------------------------------------
        [HttpPost, ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Assign(PlayeroAssignVM vm)
        {
            var dueId = GetCurrentOwnerId();

            // Guard: la playa debe ser del dueño
            var esMia = await _context.AdministraPlayas
                .AnyAsync(a => a.DueNU == dueId && a.PlyID == vm.PlayaId);
            if (!esMia)
                ModelState.AddModelError(nameof(vm.PlayaId), "No podés asignar a una playa que no administrás.");

            // Buscamos si ya existe relación (para reactivar si quedó con fecha de fin)
            var existente = await _context.Trabajos
                .FirstOrDefaultAsync(t => t.PlaNU == vm.PlaNU && t.PlyID == vm.PlayaId);

            // Si ya está vigente (FechaFin == null), no permitimos duplicar
            if (existente is not null && existente.FechaFin == null)
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
                // Alta de período nuevo
                _context.Trabajos.Add(new TrabajaEn
                {
                    PlaNU = vm.PlaNU,
                    PlyID = vm.PlayaId,
                    TrabEnActual = true,           // compatibilidad
                    FechaInicio = DateTime.Now,
                    FechaFin = null                // vigente
                });
            }
            else
            {
                // Reactivar: nuevo período
                existente.TrabEnActual = true;     // compatibilidad
                existente.FechaInicio = DateTime.Now;
                existente.FechaFin = null;
                _context.Update(existente);
            }

            await _context.SaveChangesAsync();
            TempData["Msg"] = "Playero vinculado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------
        // UNASSIGN: marcar fecha de fin (no borrar)
        // ------------------------------------------------------------
        [HttpPost, ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Unassign(int plaNU, int plyID)
        {
            var dueId = GetCurrentOwnerId();

            // Guard: la playa debe ser del dueño
            var esMia = await _context.AdministraPlayas
                .AnyAsync(a => a.DueNU == dueId && a.PlyID == plyID);
            if (!esMia) return Forbid();

            var rel = await _context.Trabajos
                .FirstOrDefaultAsync(t => t.PlaNU == plaNU && t.PlyID == plyID);
            if (rel is null) return NotFound();

            // Cerrar el período vigente
            rel.TrabEnActual = false;              // compatibilidad
            if (rel.FechaFin == null)
                rel.FechaFin = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Msg"] = "Vinculación marcada como histórica.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dueId = GetCurrentOwnerId();
            var misPlyIds = await PlyIdsDelDuenioAsync(dueId);

            // Traer SOLO relaciones vigentes (FechaFin == null) del playero en MIS playas
            var relsVigentes = await _context.Trabajos
                .Where(t => t.PlaNU == id && misPlyIds.Contains(t.PlyID) && t.FechaFin == null)
                .ToListAsync();

            foreach (var r in relsVigentes)
            {
                r.TrabEnActual = false;     // compatibilidad con código viejo
                r.FechaFin = DateTime.Now;  // cerrar período
            }

            await _context.SaveChangesAsync();

            TempData["Msg"] = "El playero ya no aparece en tus listados. Se conservó el historial (fechas de fin registradas).";
            return RedirectToAction(nameof(Index));
        }


        // ------------------------------------------------------------
        // PLAZAS: sólo playeros
        // ------------------------------------------------------------
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Playero")]
        public async Task<IActionResult> Plazas()
        {
            var usuId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var turno = await _context.Turnos
                .Include(t => t.Playa)
                .FirstOrDefaultAsync(t => t.PlaNU == usuId && t.TurFyhFin == null);

            if (turno == null)
            {
                TempData["Mensaje"] = "No tenés un turno activo.";
                TempData["MensajeCss"] = "warning";
                return RedirectToAction("Index", "Home");
            }

            var plazas = await _context.Plazas
                .Where(p => p.PlyID == turno.PlyID)
                .OrderBy(p => p.PlzNum)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.PlyID = turno.PlyID;
            return View(plazas);
        }

        // ------------------------------------------------------------
        // Toggle habilitación: sólo playeros
        // ------------------------------------------------------------
        [HttpPost, ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Playero")]
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