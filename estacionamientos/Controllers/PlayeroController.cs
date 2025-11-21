using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.ViewModels;
using BCrypt.Net;

namespace estacionamientos.Controllers
{
    // Permitir que entren tanto Duenio como Playero
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Duenio,Playero")]
    public class PlayeroController : BaseController
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
                .Where(p => p.PlyEstado == EstadoPlaya.Vigente) // Solo mostrar playas vigentes
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
        public async Task<IActionResult> Index(
            string q,
            string filterBy = "todos",
            List<string>? Nombres = null,
            List<string>? Playas = null,
            List<string>? Todos = null,
            string? remove = null)
        {

            SetBreadcrumb(
                new BreadcrumbItem { Title = "Playeros", Url = Url.Action("Index", "Playero")! }
            );
            var dueId = GetCurrentOwnerId();
            var misPlyIds = await PlyIdsDelDuenioAsync(dueId);

            // Quitar un filtro
            if (!string.IsNullOrEmpty(remove))
            {
                var parts = remove.Split(':');
                if (parts.Length >= 2)
                {
                    var key = parts[0].ToLower();
                    var val = parts[1];

                    if (key == "nombre") Nombres?.Remove(val);
                    if (key == "playa") Playas?.Remove(val);
                    if (key == "todos") Todos?.Remove(val);
                }
            }

            // INDEX: solo vínculos vigentes en playas del dueño
            var trabajosActivos = await _context.Trabajos
                .Include(t => t.Playero)
                .Include(t => t.Playa)
                .Where(t => misPlyIds.Contains(t.PlyID) && t.FechaFin == null)
                .AsNoTracking()
                .ToListAsync();

            var porPlayero = trabajosActivos
                .GroupBy(t => t.Playero.UsuNU)
                .Select(g => new PlayeroIndexVM
                {
                    Playero = g.First().Playero,
                    Playas = g.Select(x => x.Playa).Distinct().ToList()
                })
                .AsQueryable();

            // Aplicar búsqueda principal
            if (!string.IsNullOrWhiteSpace(q))
            {
                porPlayero = filterBy switch
                {
                    "nombre" => porPlayero.Where(p => p.Playero.UsuNyA.ToLower().Contains(q.ToLower())),
                    "playa" => porPlayero.Where(p => p.Playas.Any(pl => 
                        (pl.PlyNom ?? "").ToLower().Contains(q.ToLower()) ||
                        (pl.PlyCiu + " - " + pl.PlyDir).ToLower().Contains(q.ToLower()))),
                    _ => porPlayero.Where(p => 
                        p.Playero.UsuNyA.ToLower().Contains(q.ToLower()) ||
                        p.Playas.Any(pl => 
                            (pl.PlyNom ?? "").ToLower().Contains(q.ToLower()) ||
                            (pl.PlyCiu + " - " + pl.PlyDir).ToLower().Contains(q.ToLower())))
                };
            }

            // Filtros acumulados
            if (Nombres?.Any() ?? false)
            {
                porPlayero = porPlayero.Where(p => 
                    Nombres.Any(nombre => p.Playero.UsuNyA.ToLower().Contains(nombre.ToLower())));
            }

            if (Playas?.Any() ?? false)
            {
                porPlayero = porPlayero.Where(p => 
                    Playas.Any(playa => p.Playas.Any(pl => 
                        (pl.PlyNom ?? "").ToLower().Contains(playa.ToLower()) ||
                        (pl.PlyCiu + " - " + pl.PlyDir).ToLower().Contains(playa.ToLower()))));
            }

            if (Todos?.Any() ?? false)
            {
                porPlayero = porPlayero.Where(p => 
                    Todos.Any(term => 
                        p.Playero.UsuNyA.ToLower().Contains(term.ToLower()) ||
                        p.Playas.Any(pl => 
                            (pl.PlyNom ?? "").ToLower().Contains(term.ToLower()) ||
                            (pl.PlyCiu + " - " + pl.PlyDir).ToLower().Contains(term.ToLower()))));
            }

            var lista = porPlayero
                .OrderBy(vm => vm.Playero.UsuNyA)
                .ToList();

            // Obtener playas del dueño para el dropdown
            var playasDelDuenio = await _context.Playas
                .Where(p => misPlyIds.Contains(p.PlyID))
                .Select(p => new { p.PlyID, Nombre = string.IsNullOrWhiteSpace(p.PlyNom) ? $"{p.PlyCiu} - {p.PlyDir}" : p.PlyNom })
                .ToListAsync();

            var vm = new PlayeroIndexFilterVM
            {
                Playeros = lista,
                Q = q ?? "",
                FilterBy = string.IsNullOrEmpty(filterBy) ? "todos" : filterBy.ToLower(),
                Nombres = Nombres ?? new(),
                Playas = Playas ?? new(),
                Todos = Todos ?? new()
            };

            ViewBag.PlayasDelDuenio = playasDelDuenio;


            var movimientos = await _context.MovimientosPlayeros
                .Include(m => m.Playa)
                .Include(m => m.Playero)
                .Where(m => misPlyIds.Contains(m.PlyID))
                .ToListAsync();

            ViewBag.ultMovimientos = movimientos;

            return View(vm);
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
            SetBreadcrumb(
                new BreadcrumbItem { Title = "Playeros", Url = Url.Action("Index", "Playero")! },
                new BreadcrumbItem { Title = "Agregar Playero", Url = Url.Action("Create", "Playero")! }
            );

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

            // Validar que la playa esté en estado Vigente
            var playa = await _context.Playas.FindAsync(vm.PlayaId);
            if (playa != null && playa.PlyEstado != EstadoPlaya.Vigente)
            {
                ModelState.AddModelError(nameof(vm.PlayaId), "No se pueden asignar playeros a playas que estén en estado Borrador. La playa debe estar Vigente (tener al menos 1 método de pago y 1 plaza configurada).");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Playas = await SelectListPlayasDelDuenioAsync(dueId, vm.PlayaId);
                return View(vm);
            }

            // Calcular el siguiente UsuNU disponible dinámicamente:
            int nextUsuNu = Math.Max(9, (await _context.Usuarios.MaxAsync(u => u.UsuNU)) + 1);

            // Verificar que no haya colisión con el valor de UsuNU
            while (await _context.Usuarios.AnyAsync(u => u.UsuNU == nextUsuNu))
            {
                nextUsuNu++;
            }

            // Asignar el UsuNU calculado al nuevo Playero
            vm.Playero.UsuNU = nextUsuNu;

            // Agregar el nuevo Playero a la base de datos
            _context.Playeros.Add(vm.Playero);
            await _context.SaveChangesAsync();

            // Crear el trabajo en la playa (relación)
            var trabajo = new TrabajaEn
            {
                PlaNU = vm.Playero.UsuNU,
                PlyID = vm.PlayaId,
                TrabEnActual = true,
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

            SetBreadcrumb(
                new BreadcrumbItem { Title = "Playeros", Url = Url.Action("Index", "Playero")! },
                new BreadcrumbItem { Title = $"Vincular a una playa", Url = Url.Action("Create", "Playero")! }
            );

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

            // Validar que la playa esté en estado Vigente
            var playa = await _context.Playas.FindAsync(vm.PlayaId);
            if (playa != null && playa.PlyEstado != EstadoPlaya.Vigente)
            {
                ModelState.AddModelError(nameof(vm.PlayaId), "No se pueden asignar playeros a playas que estén en estado Borrador. La playa debe estar Vigente (tener al menos 1 método de pago y 1 plaza configurada).");
            }

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
            try
            {
                var dueId = GetCurrentOwnerId();
                
                // Debug logs
                System.Diagnostics.Debug.WriteLine($"Unassign called: plaNU={plaNU}, plyID={plyID}, dueId={dueId}");

            // Guard: la playa debe ser del dueño
            var esMia = await _context.AdministraPlayas
                .AnyAsync(a => a.DueNU == dueId && a.PlyID == plyID);
            if (!esMia) 
            {
                System.Diagnostics.Debug.WriteLine("Forbidden: Playero no administra esta playa");
                return Forbid();
            }

            var rel = await _context.Trabajos
                .FirstOrDefaultAsync(t => t.PlaNU == plaNU && t.PlyID == plyID && t.FechaFin == null);
            if (rel is null) 
            {
                System.Diagnostics.Debug.WriteLine("Not found: No se encontró la relación TrabajaEn");
                return NotFound();
            }

            System.Diagnostics.Debug.WriteLine($"Found relation: TrabEnActual={rel.TrabEnActual}, FechaFin={rel.FechaFin}");

            // Cerrar el período vigente
            rel.TrabEnActual = false;              // compatibilidad
            if (rel.FechaFin == null)
                rel.FechaFin = DateTime.UtcNow;

            System.Diagnostics.Debug.WriteLine($"After update: TrabEnActual={rel.TrabEnActual}, FechaFin={rel.FechaFin}");

            await _context.SaveChangesAsync();

            System.Diagnostics.Debug.WriteLine("Changes saved successfully");

            TempData["Msg"] = "Vinculación marcada como histórica.";
            return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Unassign: {ex.Message}");
                TempData["Error"] = "Error al desvincular el playero.";
                return RedirectToAction(nameof(Index));
            }
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

            SetBreadcrumb(
                new BreadcrumbItem { Title = turno.Playa.PlyNom, Url = Url.Action("DetailsPlayero", "PlayaEstacionamiento", new { id = turno.Playa.PlyID})! },
                new BreadcrumbItem { Title = "Plazas", Url = Url.Action("Plazas", "Playero")! }
            );
            var plazas = await _context.Plazas
                .Include(p => p.Clasificaciones)
                    .ThenInclude(pc => pc.Clasificacion)
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
                    PlzOcupada = _context.Ocupaciones
                        .Any(o => o.PlyID == p.PlyID && o.PlzNum == p.PlzNum && o.OcufFyhFin == null),

                    // 🔹 inicializar la colección de clasificaciones (ya no hay un solo campo)
                    Clasificaciones = p.Clasificaciones.Select(pc => new PlazaClasificacion
                    {
                        PlyID = pc.PlyID,
                        PlzNum = pc.PlzNum,
                        ClasVehID = pc.ClasVehID,
                        Clasificacion = pc.Clasificacion
                    }).ToList()
                })
                .AsNoTracking()
                .ToListAsync();


            ViewBag.PlyID = turno.PlyID;
            ViewBag.Playa = turno.Playa; // pasar datos de la playa a la vista
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
            SetBreadcrumb(
                new BreadcrumbItem { Title = "Playeros", Url = Url.Action("Index", "Playero")! },
                new BreadcrumbItem { Title = "Historial", Url = Url.Action("HistorialAgrupado", "Playero")! }
            );

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
                    PlayeroEmail = t.Playero.UsuEmail,
                    PlayeroTelefono = t.Playero.UsuNumTel,
                    PlayaNombre = string.IsNullOrWhiteSpace(t.Playa.PlyNom)
                                    ? (t.Playa.PlyCiu + " - " + t.Playa.PlyDir)
                                    : t.Playa.PlyNom,
                    FechaInicio = (DateTime?)EF.Property<DateTime?>(t, "FechaInicio"),
                    FechaFin = (DateTime?)EF.Property<DateTime?>(t, "FechaFin"),
                    Vigente = t.TrabEnActual || EF.Property<DateTime?>(t, "FechaFin") == null
                })
                .ToListAsync();

            var data = flat
                .GroupBy(x => new { x.PlaNU, x.PlayeroNombre, x.PlayeroEmail, x.PlayeroTelefono })
                .Select(g => new PlayeroHistGroupVM
                {
                    PlaNU = g.Key.PlaNU,
                    PlayeroNombre = g.Key.PlayeroNombre,
                    PlayeroEmail = g.Key.PlayeroEmail,
                    PlayeroTelefono = g.Key.PlayeroTelefono!,
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