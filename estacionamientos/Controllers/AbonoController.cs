using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.ViewModels;
using estacionamientos.ViewModels.SelectOptions;
using System.Security.Claims;



namespace estacionamientos.Controllers
{
    public class AbonoController : Controller
    {
        private readonly AppDbContext _ctx;
        public AbonoController(AppDbContext ctx) => _ctx = ctx;

        private async Task LoadSelects(int? plySel = null, int? pagSel = null)
        {
            var playas = await _ctx.Playas.AsNoTracking()
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyCiu + " - " + p.PlyDir })
                .ToListAsync();
            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", plySel);

            var pagos = plySel is null
                ? new List<OpcionPago>()
                : await _ctx.Pagos.AsNoTracking()
                    .Where(p => p.PlyID == plySel)
                    .OrderByDescending(p => p.PagFyh)
                    .Select(p => new OpcionPago { PagNum = p.PagNum, Texto = p.PagNum + " - " + p.PagFyh.ToString("g") })
                    .ToListAsync();
            ViewBag.PagNum = new SelectList(pagos, "PagNum", "Texto", pagSel);

            // 🔹 Ya no cargamos plazas ni abonados
        }

        private Task<bool> PagoExiste(int plyID, int pagNum)
            => _ctx.Pagos.AnyAsync(p => p.PlyID == plyID && p.PagNum == pagNum);

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Playero"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var turno = await _ctx.Turnos
                    .Where(t => t.PlaNU.ToString() == userId && t.TurFyhFin == null)
                    .Include(t => t.Playa)
                    .FirstOrDefaultAsync();

                if (turno == null)
                    return View("NoTurno");

                var q = _ctx.Abonos
                    .Include(a => a.Plaza).ThenInclude(p => p.Playa)
                    .Include(a => a.Abonado)
                    .Include(a => a.Pago)
                    .Where(a => a.PlyID == turno.PlyID) // solo abonos de la playa del turno
                    .AsNoTracking();

                return View(await q.ToListAsync());
            }

            // Si no es playero → muestra todos los abonos
            var qAll = _ctx.Abonos
                .Include(a => a.Plaza).ThenInclude(p => p.Playa)
                .Include(a => a.Abonado)
                .Include(a => a.Pago)
                .AsNoTracking();

            return View(await qAll.ToListAsync());
        }


        public async Task<IActionResult> Details(int plyID, int plzNum, DateTime aboFyhIni)
        {
            var item = await _ctx.Abonos
                .Include(a => a.Plaza).ThenInclude(p => p.Playa)
                .Include(a => a.Abonado)
                .Include(a => a.Pago)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.PlyID == plyID && a.PlzNum == plzNum && a.AboFyhIni == aboFyhIni);
            return item is null ? NotFound() : View(item);
        }

        public async Task<IActionResult> Create()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Playero"))
            {
                var turno = await _ctx.Turnos
                    .Where(t => t.PlaNU.ToString() == userId && t.TurFyhFin == null)
                    .FirstOrDefaultAsync();

                if (turno == null)
                {
                    TempData["Error"] = "Debe tener un turno activo para registrar abonos.";
                    return RedirectToAction(nameof(Index));
                }

                var playaNombre = await _ctx.Playas
                    .Where(p => p.PlyID == turno.PlyID)
                    .Select(p => p.PlyNom)
                    .FirstOrDefaultAsync();

                ViewBag.PlayaNombre = playaNombre;

                await LoadSelects(turno.PlyID);

                ViewBag.ClasVehID = new SelectList(
                    await _ctx.ClasificacionesVehiculo
                        .OrderBy(c => c.ClasVehTipo)  
                        .ToListAsync(),
                    "ClasVehID", "ClasVehTipo"       
                );


                return View(new AbonoCreateVM
                {
                    PlyID = turno.PlyID,
                    AboFyhIni = DateTime.UtcNow,
                    Vehiculos = new List<VehiculoVM>() 
                });

            }

            await LoadSelects();

            // 🔹 Cargar clasificaciones también aquí
            ViewBag.ClasVehID = new SelectList(
                await _ctx.ClasificacionesVehiculo
                    .OrderBy(c => c.ClasVehTipo)   // 👈 usar ClasVehTipo
                    .ToListAsync(),
                "ClasVehID", "ClasVehTipo"        // 👈 usar ClasVehTipo
            );

            return View(new AbonoCreateVM { AboFyhIni = DateTime.UtcNow });
        }


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AbonoCreateVM model)

        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Playero"))
            {
                var turno = await _ctx.Turnos
                    .Where(t => t.PlaNU.ToString() == userId && t.TurFyhFin == null)
                    .FirstOrDefaultAsync();

                if (turno == null)
                {
                    TempData["Error"] = "Debe tener un turno activo para registrar abonos.";
                    return RedirectToAction(nameof(Index));
                }

                // Forzar siempre la playa del turno activo
                model.PlyID = turno.PlyID;
            }

            if (!await PagoExiste(model.PlyID, model.PagNum))
                ModelState.AddModelError(nameof(model.PagNum), "El pago no existe para esa playa.");

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.PagNum);
                return View(model);
            }


            // 1. Abonado
            var abonado = await _ctx.Abonados.FindAsync(model.AboDNI);
            if (abonado == null)
            {
                abonado = new Abonado { AboDNI = model.AboDNI, AboNom = model.AboNom };
                _ctx.Abonados.Add(abonado);
            }

            // 2. Abono
            var abono = new Abono
            {
                PlyID = model.PlyID,
                AboFyhIni = model.AboFyhIni,
                AboFyhFin = model.AboFyhFin,
                AboDNI = model.AboDNI,
                PagNum = model.PagNum
                // 🔹 PlzNum se definirá más adelante según los vehículos y plazas disponibles
            };


            // 3. Vehículos
            foreach (var v in model.Vehiculos)
            {
                var vehiculo = await _ctx.Vehiculos.FindAsync(v.VehPtnt);
                if (vehiculo == null)
                {
                    vehiculo = new Vehiculo
                    {
                        VehPtnt = v.VehPtnt,
                        ClasVehID = v.ClasVehID
                    };

                    _ctx.Vehiculos.Add(vehiculo);
                }

                abono.Vehiculos.Add(new VehiculoAbonado
                {
                    PlyID = abono.PlyID,
                    PlzNum = abono.PlzNum,
                    AboFyhIni = abono.AboFyhIni,
                    VehPtnt = v.VehPtnt
                });
            }

            // 4. Calcular monto (ejemplo muy básico, ajustar con tarifas reales)
            if (model.AboFyhFin != null)
{
            var dias = (model.AboFyhFin.Value - model.AboFyhIni).TotalDays;

            // Determinar el servicio según cantidad de días
            int? serId = dias switch
            {
                <= 1 => 7,   // Servicio "Estacionamiento por 1 Día"
                <= 7 => 8,   // Servicio "Estacionamiento por 1 Semana"
                <= 30 => 9,  // Servicio "Estacionamiento por 1 Mes"
                _ => null
            };

            if (serId != null)
            {
                var tarifa = await _ctx.TarifasServicio
                    .Where(t => t.PlyID == model.PlyID
                            && t.SerID == serId
                            && t.ClasVehID == model.Vehiculos.First().ClasVehID
                            && (t.TasFecFin == null || t.TasFecFin >= model.AboFyhIni))
                    .OrderByDescending(t => t.TasFecIni) // usar la más reciente vigente
                    .FirstOrDefaultAsync();

            }
            else
            {
                abono.AboMonto = 0; // para duraciones fuera de rango
            }
        }

            _ctx.Abonos.Add(abono);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Edit(int plyID, int plzNum, DateTime aboFyhIni)
        {
            var item = await _ctx.Abonos.FindAsync(plyID, plzNum, aboFyhIni);
            if (item is null) return NotFound();
            await LoadSelects(item.PlyID, item.PagNum);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int plzNum, DateTime aboFyhIni, Abono model)
        {
            if (plyID != model.PlyID || plzNum != model.PlzNum || aboFyhIni != model.AboFyhIni) return BadRequest();

            if (!await PagoExiste(model.PlyID, model.PagNum))
                ModelState.AddModelError(nameof(model.PagNum), "El pago no existe para esa playa.");
            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.PagNum);

                ViewBag.ClasVehID = new SelectList(
                    await _ctx.ClasificacionesVehiculo
                        .OrderBy(c => c.ClasVehTipo)
                        .ToListAsync(),
                    "ClasVehID", "ClasVehTipo"
                );

                return View(model);
            }


            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int plyID, int plzNum, DateTime aboFyhIni)
        {
            var item = await _ctx.Abonos
                .Include(a => a.Abonado).Include(a => a.Plaza).Include(a => a.Pago)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.PlyID == plyID && a.PlzNum == plzNum && a.AboFyhIni == aboFyhIni);
            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int plzNum, DateTime aboFyhIni)
        {
            var item = await _ctx.Abonos.FindAsync(plyID, plzNum, aboFyhIni);
            if (item is null) return NotFound();
            _ctx.Abonos.Remove(item);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
