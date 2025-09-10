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
                FechaInicio = DateTime.UtcNow,
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

            // 1) Guard: la playa debe ser del dueño
            var esMia = await _context.AdministraPlayas
                .AnyAsync(a => a.DueNU == dueId && a.PlyID == vm.PlayaId);
            if (!esMia)
                ModelState.AddModelError(nameof(vm.PlayaId), "No podés asignar a una playa que no administrás.");

            // 2) ¿ya está vigente en ESTA playa?
            var vigenteMisma = await _context.Trabajos
                .FirstOrDefaultAsync(t => t.PlaNU == vm.PlaNU
                                       && t.PlyID == vm.PlayaId
                                       && t.FechaFin == null);
            if (vigenteMisma is not null)
                ModelState.AddModelError(nameof(vm.PlayaId), "El playero ya está activo en esa playa.");

            if (!ModelState.IsValid)
            {
                ViewBag.Playas = await SelectListPlayasDelDuenioAsync(dueId, vm.PlayaId);
                var p = await _context.Playeros.AsNoTracking().FirstOrDefaultAsync(x => x.UsuNU == vm.PlaNU);
                vm.PlayeroNombre = p?.UsuNyA;
                return View(vm);
            }

            // 3) Crear un NUEVO período para (PlyID, PlaNU) con nuevo FechaInicio
            var nuevo = new TrabajaEn
            {
                PlyID = vm.PlayaId,
                PlaNU = vm.PlaNU,
                TrabEnActual = true,                      // legado (seguí mostrándolo si querés)
                FechaInicio = DateTime.UtcNow,
                FechaFin = null
            };
            _context.Trabajos.Add(nuevo);

            await _context.SaveChangesAsync();

            TempData["Msg"] = "Playero vinculado correctamente (nuevo período).";
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
                rel.FechaFin = DateTime.UtcNow;

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
                r.FechaFin = DateTime.UtcNow;  // cerrar período
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
                .Include(p => p.Clasificacion)    // 🔹 incluí la relación Clasificación
                .Where(p => p.PlyID == turno.PlyID)
                .OrderBy(p => p.PlzNum)
                .Select(p => new PlazaEstacionamiento
                {
                    PlyID = p.PlyID,
                    PlzNum = p.PlzNum,
                    PlzNombre = p.PlzNombre,
                    PlzTecho = p.PlzTecho,
                    PlzAlt = p.PlzAlt,
                    PlzHab = p.PlzHab,
                    ClasVehID = p.ClasVehID,
                    Clasificacion = p.Clasificacion,
                    // 🔹 Estado dinámico: ocupado si hay Ocupación activa
                    PlzOcupada = _context.Ocupaciones
                        .Any(o => o.PlyID == p.PlyID && o.PlzNum == p.PlzNum && o.OcufFyhFin == null)
                })
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

            // 🚨 Validación: no permitir inhabilitar una plaza ocupada
            if (plaza.PlzOcupada && plaza.PlzHab)
            {
                TempData["Mensaje"] = $"No se puede inhabilitar la plaza {plaza.PlzNum} porque está ocupada.";
                TempData["MensajeCss"] = "danger";
                return RedirectToAction(nameof(Plazas));
            }

            plaza.PlzHab = !plaza.PlzHab;
            _context.Update(plaza);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Plaza {plaza.PlzNum} {(plaza.PlzHab ? "habilitada" : "deshabilitada")}.";
            TempData["MensajeCss"] = plaza.PlzHab ? "success" : "warning";

            return RedirectToAction(nameof(Plazas));
        }


        // ------------------------------------------------------------
        // HISTORIAL: todos los períodos (vigentes e históricos) de mis playeros
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> HistorialAgrupado()
            {
                var dueId = GetCurrentOwnerId();                          // helper tuyo
                var misPlyIds = await PlyIdsDelDuenioAsync(dueId);        // helper tuyo

                var q = _context.Trabajos
                    .Include(t => t.Playero)
                    .Include(t => t.Playa)
                    .Where(t => misPlyIds.Contains(t.PlyID))
                    .AsNoTracking();

                // Si ya añadiste FechaInicio/FechaFin al modelo, podés usar t.FechaInicio / t.FechaFin directamente.
                var flat = await q
                    .Select(t => new
                    {
                        t.PlaNU,
                        PlayeroNombre = t.Playero.UsuNyA,
                        PlayaNombre = string.IsNullOrWhiteSpace(t.Playa.PlyNom)
                                        ? (t.Playa.PlyCiu + " - " + t.Playa.PlyDir)
                                        : t.Playa.PlyNom,
                        FechaInicio = (DateTime?)EF.Property<DateTime?>(t, "FechaInicio"),
                        FechaFin = (DateTime?)EF.Property<DateTime?>(t, "FechaFin"),
                        Vigente = t.TrabEnActual || EF.Property<DateTime?>(t, "FechaFin") == null
                    })
                    .ToListAsync();

                var data = flat
                    .GroupBy(x => new { x.PlaNU, x.PlayeroNombre })
                    .Select(g => new PlayeroHistGroupVM
                    {
                        PlaNU = g.Key.PlaNU,
                        PlayeroNombre = g.Key.PlayeroNombre,
                        Periodos = g.OrderByDescending(p => p.Vigente)
                                    .ThenByDescending(p => p.FechaInicio)
                                    .Select(p => new PeriodoVM
                                    {
                                        PlayaNombre = p.PlayaNombre,
                                        FechaInicio = p.FechaInicio,
                                        FechaFin = p.FechaFin,
                                        Vigente = p.Vigente && p.FechaFin is null
                                    })
                                    .ToList()
                    })
                    .OrderBy(vm => vm.PlayeroNombre)
                    .ToList();

                return View(data);
            }
    }
}