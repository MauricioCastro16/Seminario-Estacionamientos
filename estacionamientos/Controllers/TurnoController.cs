using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using System.Security.Claims;

namespace estacionamientos.Controllers
{
    public class TurnoController : Controller
    {
        private readonly AppDbContext _ctx;
        public TurnoController(AppDbContext ctx) => _ctx = ctx;

        // -------------------- Helpers --------------------

        private int GetCurrentPlaNU()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var plaNU) ? plaNU : 0;
        }

        private Task<bool> TrabajaEnAsync(int plyID, int plaNU)
        => _ctx.Trabajos.AnyAsync(t =>
            t.PlyID == plyID &&
            t.PlaNU == plaNU &&
            t.TrabEnActual &&
            t.FechaFin == null);

        private static DateTime ToUtc(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Unspecified)
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Local);
            return dt.ToUniversalTime();
        }

        private static decimal? ParseMoney(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            raw = raw.Trim().Replace(",", ".");
            return decimal.TryParse(raw,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var val) ? val : null;
        }

        private async Task LoadSelects(int? plaSel = null, int? plySel = null, int? filterPlaNU = null)
        {
            var playerosQuery = _ctx.Playeros.AsNoTracking()
                .OrderBy(p => p.UsuNyA)
                .Select(p => new { p.UsuNU, p.UsuNyA });

            if (filterPlaNU is int onlyPla && onlyPla > 0)
                playerosQuery = playerosQuery.Where(p => p.UsuNU == onlyPla);

            ViewBag.PlaNU = new SelectList(await playerosQuery.ToListAsync(), "UsuNU", "UsuNyA", plaSel);

            var playasQuery = _ctx.Playas.AsNoTracking()
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyNom + " (" + p.PlyCiu + ")" });

            if (filterPlaNU is int filterPla && filterPla > 0)
            {
                var plyIDs = await _ctx.Trabajos
                    .AsNoTracking()
                    .Where(t =>
                        t.PlaNU == filterPla &&
                        t.TrabEnActual &&
                        t.FechaFin == null)
                    .Select(t => t.PlyID)
                    .Distinct()
                    .ToListAsync();

                playasQuery = playasQuery.Where(p => plyIDs.Contains(p.PlyID));
            }

            ViewBag.PlyID = new SelectList(await playasQuery.ToListAsync(), "PlyID", "Nombre", plySel);
        }

        // -------------------- Acciones --------------------

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Playero"))
            {
                var plaNU = GetCurrentPlaNU();

                var abierto = await _ctx.Turnos
                    .Include(t => t.Playa)
                    .AsNoTracking()
                    .Where(t => t.PlaNU == plaNU && t.TurFyhFin == null)
                    .OrderByDescending(t => t.TurFyhIni)
                    .FirstOrDefaultAsync();

                var ultimos = await _ctx.Turnos
                    .Include(t => t.Playa)
                    .AsNoTracking()
                    .Where(t => t.PlaNU == plaNU && t.TurFyhFin != null)
                    .OrderByDescending(t => t.TurFyhIni)
                    .Take(10)
                    .ToListAsync();

                ViewBag.TurnoAbierto = abierto;
                return View(ultimos);
            }

            var q = _ctx.Turnos
                .Include(t => t.Playero)
                .Include(t => t.Playa)
                .AsNoTracking();

            return View(await q.ToListAsync());
        }

        public async Task<IActionResult> Details(int plyID, int plaNU, DateTime turFyhIni)
        {
            // Usuario actual
            var currentPlaNU = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            
            // DEBUG: Log para entender qué está pasando
            System.Diagnostics.Debug.WriteLine($"Details - Usuario logueado: {currentPlaNU}, plaNU parámetro: {plaNU}, Es playero: {User.IsInRole("Playero")}");

            // Si es playero, SOLO puede ver sus propios turnos
            if (User.IsInRole("Playero"))
            {
                if (plaNU != currentPlaNU)
                {
                    System.Diagnostics.Debug.WriteLine($"ACCESO DENEGADO - Playero {currentPlaNU} intentó ver turno de playero {plaNU}");
                    TempData["Error"] = "No tienes permiso para ver este turno.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Cargar turno con sus relaciones
            var turno = await _ctx.Turnos
                .Include(t => t.Playa)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.PlyID == plyID 
                                    && t.PlaNU == plaNU 
                                    && t.TurFyhIni == turFyhIni);

            if (turno is null) return NotFound();

            // Doble verificación: si es playero, asegurar que el turno sea suyo
            if (User.IsInRole("Playero") && turno.PlaNU != currentPlaNU)
            {
                return Forbid();
            }

            // 🔑 Cargar el playero del turno (no el logueado)
            var playero = await _ctx.Playeros
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UsuNU == turno.PlaNU);

            if (playero != null)
            {
                turno.Playero = playero;
            }
            
            // Obtener desglose de pagos por método de pago durante ESE turno específico
            var desglosePagos = await _ctx.Pagos
                .Include(p => p.MetodoPago)
                .Where(p => p.PlyID == plyID 
                        && p.PlaNU == plaNU 
                        && p.PagFyh >= turno.TurFyhIni 
                        && (turno.TurFyhFin == null || p.PagFyh <= turno.TurFyhFin))
                .AsNoTracking()
                .ToListAsync();

            var agrupados = desglosePagos
                .GroupBy(p => new { p.MepID, MetodoPago = p.MetodoPago?.MepNom ?? "Desconocido" })
                .Select(g => new
                {
                    MetodoPago = g.Key.MetodoPago,
                    CantidadPagos = g.Count(),
                    MontoTotal = g.Sum(p => p.PagMonto)
                })
                .OrderByDescending(x => x.MontoTotal)
                .ToList();

            var totalPagos = agrupados.Sum(x => x.MontoTotal);
            var cantidadTotalPagos = agrupados.Sum(x => x.CantidadPagos);

            ViewBag.DesglosePagos = agrupados;
            ViewBag.TotalPagos = totalPagos;
            ViewBag.CantidadTotalPagos = cantidadTotalPagos;

            return View(turno);
        }

        // --- CAMBIO mínimo: aceptar returnUrl y guardarlo en ViewBag ---
        public async Task<IActionResult> Create(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (User.IsInRole("Playero"))
            {
                var plaNU = GetCurrentPlaNU();

                var yaAbierto = await _ctx.Turnos
                    .AnyAsync(t => t.PlaNU == plaNU && t.TurFyhFin == null);

                if (yaAbierto)
                {
                    TempData["Error"] = "Ya tenés un turno en curso.";
                    if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    return RedirectToAction(nameof(Index));
                }

                await LoadSelects(plaSel: plaNU, plySel: null, filterPlaNU: plaNU);
                return View(new Turno { PlaNU = plaNU, TurFyhIni = DateTime.Now });
            }

            await LoadSelects();
            return View(new Turno { TurFyhIni = DateTime.Now });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Turno model)
        {
            model.TurApertCaja = ParseMoney(Request.Form[nameof(model.TurApertCaja)]);
            if (ModelState.ContainsKey(nameof(model.TurApertCaja)))
                ModelState[nameof(model.TurApertCaja)]!.Errors.Clear();

            ModelState.Remove(nameof(model.TrabajaEn));
            ModelState.Remove(nameof(model.Playa));
            ModelState.Remove(nameof(model.Playero));

            if (User.IsInRole("Playero"))
            {
                var plaNU = GetCurrentPlaNU();
                model.PlaNU = plaNU;
                ModelState.Remove(nameof(model.PlaNU));

                if (await _ctx.Turnos.AnyAsync(t => t.PlaNU == plaNU && t.TurFyhFin == null))
                    ModelState.AddModelError(string.Empty, "Ya tenés un turno en curso.");

                if (!await TrabajaEnAsync(model.PlyID, plaNU))
                    ModelState.AddModelError(string.Empty, "No trabajás en esa playa.");

                // SIEMPRE guardar turnos en UTC
                model.TurFyhIni = DateTime.UtcNow;
            }
            else
            {
                if (!await TrabajaEnAsync(model.PlyID, model.PlaNU))
                    ModelState.AddModelError(string.Empty, "El playero no trabaja en esa playa.");

                model.TurFyhIni = model.TurFyhIni == default ? DateTime.UtcNow : ToUtc(model.TurFyhIni);
            }

            // >>>>>> NUEVO: resolver período (TrabajaEn) y setear la FK (TrabFyhIni) <<<<<<
            if (ModelState.IsValid)
            {
                // Buscar el período vigente del playero en esa playa
                var periodo = await _ctx.Trabajos
                    .Where(t =>
                        t.PlyID == model.PlyID &&
                        t.PlaNU == model.PlaNU &&
                        t.TrabEnActual &&
                        t.FechaFin == null)
                    .OrderByDescending(t => t.FechaInicio)
                    .FirstOrDefaultAsync();

                if (periodo == null)
                {
                    ModelState.AddModelError(string.Empty,
                        "No hay un período vigente (TrabajaEn) para ese playero en esa playa.");
                }
                else
                {
                    model.TrabFyhIni = periodo.FechaInicio; // FK a período
                }

            }

            if (!ModelState.IsValid)
            {
                if (User.IsInRole("Playero"))
                    await LoadSelects(model.PlaNU, model.PlyID, filterPlaNU: model.PlaNU);
                else
                    await LoadSelects(model.PlaNU, model.PlyID);
                return View(model);
            }

            _ctx.Turnos.Add(model);
            await _ctx.SaveChangesAsync();

            TempData["Ok"] = " Turno iniciado.";

            var returnUrl = Request.Form["returnUrl"].FirstOrDefault() ?? Request.Query["returnUrl"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int plyID, int plaNU, DateTime turFyhIni, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            var item = await _ctx.Turnos
                .Include(t => t.Playa)
                .FirstOrDefaultAsync(t => t.PlyID == plyID && t.PlaNU == plaNU && t.TurFyhIni == turFyhIni);

            if (item is null) return NotFound();
            if (User.IsInRole("Playero") && plaNU != GetCurrentPlaNU())
                return Forbid();

            if (User.IsInRole("Playero"))
                await LoadSelects(item.PlaNU, item.PlyID, filterPlaNU: item.PlaNU);
            else
                await LoadSelects(item.PlaNU, item.PlyID);

            ViewBag.NowLocal = DateTime.Now;
            
            // 🔹 Calcular efectivo esperado para mostrar en la vista
            if (User.IsInRole("Playero"))
            {
                var esperado = await _ctx.Pagos
                    .Include(p => p.MetodoPago)
                    .Where(p => p.PlyID == item.PlyID
                            && p.PlaNU == item.PlaNU
                            && p.PagFyh >= item.TurFyhIni
                            && (item.TurFyhFin == null || p.PagFyh <= item.TurFyhFin)
                            && p.MetodoPago.MepNom == "Efectivo")
                    .SumAsync(p => (decimal?)p.PagMonto) ?? 0m;

                ViewBag.EfectivoEsperado = esperado;
            }
            else
            {
                ViewBag.EfectivoEsperado = 0;
            }
            
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int plaNU, DateTime turFyhIni, Turno model)
        {
            if (plyID != model.PlyID || plaNU != model.PlaNU || turFyhIni != model.TurFyhIni)
                return BadRequest();

            if (User.IsInRole("Playero") && plaNU != GetCurrentPlaNU())
                return Forbid();

            var db = await _ctx.Turnos
                .Include(t => t.Playa)
                .FirstOrDefaultAsync(t => t.PlyID == plyID && t.PlaNU == plaNU && t.TurFyhIni == turFyhIni);

            if (db is null) return NotFound();

            var parsedCierre = ParseMoney(Request.Form[nameof(model.TurCierrCaja)]);
            if (ModelState.ContainsKey(nameof(model.TurCierrCaja)))
                ModelState[nameof(model.TurCierrCaja)]!.Errors.Clear();

            ModelState.Remove(nameof(model.TrabajaEn));
            ModelState.Remove(nameof(model.Playa));
            ModelState.Remove(nameof(model.Playero));

            if (!await TrabajaEnAsync(db.PlyID, db.PlaNU))
                ModelState.AddModelError(string.Empty, "El playero no trabaja en esa playa.");

            if (!ModelState.IsValid)
            {
                if (User.IsInRole("Playero"))
                    await LoadSelects(db.PlaNU, db.PlyID, filterPlaNU: db.PlaNU);
                else
                    await LoadSelects(db.PlaNU, db.PlyID);

                ViewBag.NowLocal = DateTime.Now;
                return View(db);
            }

            if (User.IsInRole("Playero"))
            {
                db.TurFyhFin = DateTime.UtcNow;
                db.TurCierrCaja = parsedCierre;

                _ctx.Update(db);
                await _ctx.SaveChangesAsync();
                
                TempData["Ok"] = "Turno cerrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                db.TurApertCaja = ParseMoney(Request.Form[nameof(model.TurApertCaja)]);
                db.TurCierrCaja = parsedCierre;
                db.TurFyhIni = ToUtc(model.TurFyhIni);
                db.TurFyhFin = model.TurFyhFin.HasValue ? ToUtc(model.TurFyhFin.Value) : null;
            }

            _ctx.Update(db);
            await _ctx.SaveChangesAsync();
            TempData["Ok"] = " Turno actualizado.";

            // --- CAMBIO mínimo: respetar returnUrl (query o form) ---
            var returnUrl = Request.Form["returnUrl"].FirstOrDefault() ?? Request.Query["returnUrl"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int plyID, int plaNU, DateTime turFyhIni)
        {
            var item = await _ctx.Turnos
                .Include(t => t.Playero)
                .Include(t => t.Playa)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.PlyID == plyID && t.PlaNU == plaNU && t.TurFyhIni == turFyhIni);

            if (item is null) return NotFound();
            if (User.IsInRole("Playero") && plaNU != GetCurrentPlaNU())
                return Forbid();

            return View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int plaNU, DateTime turFyhIni)
        {
            if (User.IsInRole("Playero") && plaNU != GetCurrentPlaNU())
                return Forbid();

            var item = await _ctx.Turnos.FindAsync(plyID, plaNU, turFyhIni);
            if (item is null) return NotFound();

            _ctx.Turnos.Remove(item);
            await _ctx.SaveChangesAsync();
            TempData["Ok"] = " Turno eliminado.";
            return RedirectToAction(nameof(Index));
        }
    }
}