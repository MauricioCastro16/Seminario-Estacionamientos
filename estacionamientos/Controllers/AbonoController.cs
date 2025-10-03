using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.ViewModels;
using estacionamientos.ViewModels.SelectOptions;
using estacionamientos.Helpers;
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

            // Servicios de abono disponibles (según seed: 7=1 día, 8=1 semana, 9=1 mes)
            var serviciosAbono = await _ctx.Servicios
                .Where(s => (s.SerNom == "Abono por 1 día") || (s.SerNom == "Abono por 1 semana") || (s.SerNom == "Abono por 1 mes"))
                .OrderBy(s => s.SerID)
                .Select(s => new { s.SerID, s.SerNom, s.SerDuracionMinutos })
                .ToListAsync();
            ViewBag.ServiciosAbono = new SelectList(serviciosAbono, "SerID", "SerNom");

            // Métodos de pago ya no se cargan - se asigna por defecto

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
                    .Include(a => a.Pago).ThenInclude(p => p.MetodoPago)
                    .Include(a => a.Vehiculos).ThenInclude(v => v.Vehiculo).ThenInclude(v => v.Clasificacion)
                    .Where(a => a.PlyID == turno.PlyID) // solo abonos de la playa del turno
                    .AsNoTracking();

                return View(await q.ToListAsync());
            }

            // Si no es playero → muestra todos los abonos
            var qAll = _ctx.Abonos
                .Include(a => a.Plaza).ThenInclude(p => p.Playa)
                .Include(a => a.Abonado)
                .Include(a => a.Pago).ThenInclude(p => p.MetodoPago)
                .Include(a => a.Vehiculos).ThenInclude(v => v.Vehiculo).ThenInclude(v => v.Clasificacion)
                .AsNoTracking();

            return View(await qAll.ToListAsync());
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

            // Asignar método de pago por defecto (efectivo)
            if (model.MepID == 0)
            {
                model.MepID = 1; // Asumir que ID 1 es efectivo, ajustar según tu base de datos
            }

            if (model.SelectedPlzNum == null || model.SelectedPlzNum == 0)
            {
                ModelState.AddModelError(nameof(model.SelectedPlzNum), ErrorMessages.SeleccionePlaza);
                // Debug: verificar que el error se está agregando
                System.Diagnostics.Debug.WriteLine($"SelectedPlzNum value: {model.SelectedPlzNum}");
            }

            // Validar que haya al menos un vehículo
            if (model.Vehiculos == null || model.Vehiculos.Count == 0)
            {
                ModelState.AddModelError(nameof(model.Vehiculos), "Debe agregar al menos un vehículo para el abono.");
            }
            else
            {
                // Validar que todos los vehículos tengan patente
                for (int i = 0; i < model.Vehiculos.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(model.Vehiculos[i].VehPtnt))
                    {
                        ModelState.AddModelError($"Vehiculos[{i}].VehPtnt", "La patente es obligatoria para todos los vehículos.");
                    }
                }
            }

            // Validar que existan tarifas configuradas para esta clasificación y servicio
            var tieneTarifa = await _ctx.TarifasServicio
                .AnyAsync(t => t.PlyID == model.PlyID
                            && t.ClasVehID == model.ClasVehID
                            && t.SerID == model.SerID
                            && (t.TasFecFin == null || t.TasFecFin >= DateTime.UtcNow));

            if (!tieneTarifa)
            {
                return Json(new { 
                    error = true, 
                    message = "No existen tarifas de abono configuradas para esta clasificación de vehículo" 
                });
            }


            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, null);
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
                AboFyhIni = DateTime.SpecifyKind(model.AboFyhIni, DateTimeKind.Utc),
                AboFyhFin = model.AboFyhFin.HasValue ? DateTime.SpecifyKind(model.AboFyhFin.Value, DateTimeKind.Utc) : null,
                AboDNI = model.AboDNI,
                EstadoPago = EstadoPago.Activo,
                // PagNum se asignará luego del Pago
            };


            // 3. Vehículos
            foreach (var v in model.Vehiculos ?? new List<VehiculoVM>())
            {
                var vehiculo = await _ctx.Vehiculos.FindAsync(v.VehPtnt);
                if (vehiculo == null)
                {
                    vehiculo = new Vehiculo
                    {
                        VehPtnt = v.VehPtnt,
                        ClasVehID = model.ClasVehID
                    };

                    _ctx.Vehiculos.Add(vehiculo);
                }

                abono.Vehiculos.Add(new VehiculoAbonado
                {
                    PlyID = abono.PlyID,
                    PlzNum = abono.PlzNum,
                    AboFyhIni = DateTime.SpecifyKind(abono.AboFyhIni, DateTimeKind.Utc),
                    VehPtnt = v.VehPtnt
                });
            }

            // 4. Calcular monto y fechas por servicio seleccionado (SerID) y clase del primer vehículo
            if (model.SerID.HasValue)
            {
                var clasVehId = model.ClasVehID;
                var tarifa = await _ctx.TarifasServicio
                    .Where(t => t.PlyID == model.PlyID
                             && t.SerID == model.SerID.Value
                             && t.ClasVehID == clasVehId
                             && (t.TasFecFin == null || t.TasFecFin >= DateTime.SpecifyKind(model.AboFyhIni, DateTimeKind.Utc)))
                    .OrderByDescending(t => t.TasFecIni)
                    .FirstOrDefaultAsync();

                // Duración base del servicio => calcular fin en base a Periodos
                var servicio = await _ctx.Servicios.AsNoTracking().FirstOrDefaultAsync(s => s.SerID == model.SerID.Value);
                int diasBase;
                if (servicio?.SerDuracionMinutos != null)
                {
                    diasBase = (int)Math.Ceiling(servicio.SerDuracionMinutos.Value / 1440m);
                }
                else
                {
                    diasBase = model.SerID.Value switch { 7 => 1, 8 => 7, 9 => 30, _ => 0 };
                }

                var periodos = Math.Max(1, model.Periodos);
                var inicioUtc = DateTime.SpecifyKind(model.AboFyhIni, DateTimeKind.Utc);
                var finUtc = DateTime.SpecifyKind(inicioUtc.AddDays(diasBase * periodos), DateTimeKind.Utc);
                abono.AboFyhIni = inicioUtc;
                abono.AboFyhFin = finUtc;

                var montoUnitario = tarifa?.TasMonto ?? 0m;
                abono.AboMonto = montoUnitario * periodos;
            }
            else
            {
                abono.AboMonto = 0m;
            }

            // 5. Crear Pago (siempre se paga al generar el abono)
            var nextPagNum = (_ctx.Pagos.Where(p => p.PlyID == model.PlyID).Select(p => (int?)p.PagNum).Max() ?? 0) + 1;
            var pago = new Pago
            {
                PlyID = model.PlyID,
                PagNum = nextPagNum,
                MepID = model.MepID,
                PagMonto = abono.AboMonto,
                PagFyh = DateTime.UtcNow
            };
            _ctx.Pagos.Add(pago);
            await _ctx.SaveChangesAsync();

            abono.PagNum = pago.PagNum;
            // 6. Asignar y marcar plaza
            if (model.SelectedPlzNum == null || model.SelectedPlzNum == 0)
            {
                // intentar elegir la primera disponible si no se seleccionó
                var plazaAuto = await _ctx.Plazas
                    .Where(p => p.PlyID == model.PlyID && p.PlzHab && !p.PlzOcupada)
                    .Join(_ctx.PlazasClasificaciones,
                        p => new { p.PlyID, p.PlzNum },
                        pc => new { pc.PlyID, pc.PlzNum },
                        (p, pc) => new { p, pc })
                    .Where(x => x.pc.ClasVehID == model.ClasVehID)
                    .Select(x => x.p)
                    .OrderBy(p => p.Piso).ThenBy(p => p.PlzNum)
                    .FirstOrDefaultAsync();
                if (plazaAuto != null) model.SelectedPlzNum = plazaAuto.PlzNum;
            }

            abono.PlzNum = model.SelectedPlzNum ?? 0;
            _ctx.Abonos.Add(abono);
            await _ctx.SaveChangesAsync();

            // Marcar plaza como ocupada por abono (sin crear Ocupacion)
            var plaza = await _ctx.Plazas.FirstOrDefaultAsync(p => p.PlyID == model.PlyID && p.PlzNum == abono.PlzNum);
            if (plaza != null)
            {
                plaza.PlzOcupada = true;
                _ctx.Update(plaza);
                await _ctx.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));


        }

        // API: devuelve duración en días y monto vigente para serID, plyID y clasVehID
        [HttpGet]
        public async Task<IActionResult> GetAbonoInfo(int plyId, int serId, int clasVehId)
        {
            // duración en días a partir de SerDuracionMinutos
            var servicio = await _ctx.Servicios.AsNoTracking().FirstOrDefaultAsync(s => s.SerID == serId);
            int duracionDias = 0;
            if (servicio?.SerDuracionMinutos != null)
            {
                var minutos = servicio.SerDuracionMinutos.Value;
                duracionDias = (int)Math.Ceiling(minutos / 1440m);
            }
            else
            {
                // fallback según IDs conocidos
                duracionDias = serId switch { 7 => 1, 8 => 7, 9 => 30, _ => 0 };
            }

            var tarifa = await _ctx.TarifasServicio
                .Where(t => t.PlyID == plyId && t.SerID == serId && t.ClasVehID == clasVehId && (t.TasFecFin == null || t.TasFecFin >= DateTime.UtcNow))
                .OrderByDescending(t => t.TasFecIni)
                .Select(t => t.TasMonto)
                .FirstOrDefaultAsync();

            return Json(new { duracionDias, monto = tarifa });
        }

        // API: plazas disponibles por filtros
        [HttpGet]
        public async Task<IActionResult> GetPlazasDisponibles(int plyId, int clasVehId, bool? techo, int? piso, int serId)
        {
                        
            var tieneTarifa = await _ctx.TarifasServicio
                .AnyAsync(t => t.PlyID == plyId
                            && t.SerID == serId
                            && t.ClasVehID == clasVehId
                            && (t.TasFecFin == null || t.TasFecFin >= DateTime.UtcNow));


            if (!tieneTarifa)
            {
                return Json(new { 
                    error = true, 
                    message = "No existen tarifas de abono configuradas para esta clasificación de vehículo" 
                });
            }

            // Plazas hábiles, no ocupadas y que permitan la clasVehId (por PlazaClasificacion)
            var q = _ctx.Plazas
                .Where(p => p.PlyID == plyId && p.PlzHab && !p.PlzOcupada)
                .Join(_ctx.PlazasClasificaciones,
                    p => new { p.PlyID, p.PlzNum },
                    pc => new { pc.PlyID, pc.PlzNum },
                    (p, pc) => new { p, pc })
                .Where(x => x.pc.ClasVehID == clasVehId)
                .Select(x => x.p)
                .AsQueryable();

            if (techo.HasValue) q = q.Where(p => p.PlzTecho == techo.Value);
            if (piso.HasValue) q = q.Where(p => p.Piso == piso.Value);

            var plazas = await q
                .OrderBy(p => p.Piso).ThenBy(p => p.PlzNum)
                .Select(p => new { p.PlzNum, p.Piso, p.PlzTecho, p.PlzNombre })
                .ToListAsync();

            return Json(plazas);
        }


        public async Task<IActionResult> Details(int plyID, int plzNum, DateTime aboFyhIni)
        {
            var item = await _ctx.Abonos
                .Include(a => a.Abonado)
                .Include(a => a.Plaza)
                .Include(a => a.Pago)
                    .ThenInclude(p => p.MetodoPago)
                .Include(a => a.Vehiculos)
                    .ThenInclude(v => v.Vehiculo)
                        .ThenInclude(v => v.Clasificacion)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.PlyID == plyID && a.PlzNum == plzNum && a.AboFyhIni == aboFyhIni);
            
            if (item is null) return NotFound();
            return View(item);
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

        [HttpPost]
        public async Task<IActionResult> ConfirmarPago([FromBody] ConfirmarPagoAbonoVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Datos inválidos" });
                }

                using var transaction = await _ctx.Database.BeginTransactionAsync();

                // 1. Crear o verificar abonado
                var abonado = await _ctx.Abonados.FindAsync(model.AboDNI);
                if (abonado == null)
                {
                    abonado = new Abonado
                    {
                        AboDNI = model.AboDNI,
                        AboNom = model.AboNom
                    };
                    _ctx.Abonados.Add(abonado);
                    await _ctx.SaveChangesAsync();
                }

                // 2. Obtener el siguiente número de pago para la playa
                var ultimoPago = await _ctx.Pagos
                    .Where(p => p.PlyID == model.PlyID)
                    .OrderByDescending(p => p.PagNum)
                    .FirstOrDefaultAsync();
                
                int nuevoPagNum = (ultimoPago?.PagNum ?? 0) + 1;

                // 3. Crear el registro de pago
                var pago = new Pago
                {
                    PlyID = model.PlyID,
                    PagNum = nuevoPagNum,
                    MepID = model.MepID,
                    PagMonto = model.MontoPagar,
                    PagFyh = DateTime.UtcNow
                };
                _ctx.Pagos.Add(pago);
                await _ctx.SaveChangesAsync();

                // 4. Crear el abono
                var abono = new Abono
                {
                    PlyID = model.PlyID,
                    PlzNum = model.SelectedPlzNum,
                    AboFyhIni = model.AboFyhIni,
                    AboFyhFin = model.AboFyhFin,
                    AboMonto = model.AboMonto,
                    AboDNI = model.AboDNI,
                    PagNum = nuevoPagNum,
                    EstadoPago = EstadoPago.Activo

                };
                _ctx.Abonos.Add(abono);
                await _ctx.SaveChangesAsync();

                // 5. Crear o verificar vehículos y asociarlos al abono
                foreach (var vehiculoVM in model.Vehiculos)
                {
                    // Verificar si el vehículo existe
                    var vehiculo = await _ctx.Vehiculos.FindAsync(vehiculoVM.VehPtnt);
                    if (vehiculo == null)
                    {
                        // Crear nuevo vehículo con la clasificación seleccionada
                        vehiculo = new Vehiculo
                        {
                            VehPtnt = vehiculoVM.VehPtnt,
                            ClasVehID = model.ClasVehID
                        };
                        _ctx.Vehiculos.Add(vehiculo);
                        await _ctx.SaveChangesAsync();
                    }

                    // Verificar si ya existe la asociación VehiculoAbonado
                    var vehiculoAbonadoExistente = await _ctx.VehiculosAbonados
                        .FirstOrDefaultAsync(va => va.PlyID == model.PlyID && 
                                                   va.PlzNum == model.SelectedPlzNum && 
                                                   va.AboFyhIni == model.AboFyhIni && 
                                                   va.VehPtnt == vehiculoVM.VehPtnt);
                    
                    if (vehiculoAbonadoExistente == null)
                    {
                        // Asociar vehículo al abono solo si no existe
                        var vehiculoAbonado = new VehiculoAbonado
                        {
                            PlyID = model.PlyID,
                            PlzNum = model.SelectedPlzNum,
                            AboFyhIni = model.AboFyhIni,
                            VehPtnt = vehiculoVM.VehPtnt
                        };
                        _ctx.VehiculosAbonados.Add(vehiculoAbonado);
                    }
                }

                await _ctx.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Abono registrado exitosamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error interno: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMetodosPago(int plyId)
        {
            try
            {
                var metodosPago = await _ctx.AceptaMetodosPago
                    .Where(a => a.PlyID == plyId && a.AmpHab && a.MetodoPago != null)
                    .Select(a => new { a.MetodoPago.MepID, a.MetodoPago.MepNom })
                    .OrderBy(m => m.MepNom)
                    .ToListAsync();

                return Json(metodosPago);
            }
            catch (Exception)
            {
                return Json(new List<object>());
            }
        }
    }
}
