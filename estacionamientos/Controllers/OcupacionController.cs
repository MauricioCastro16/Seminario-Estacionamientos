using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.Models.ViewModels;
using System.Security.Claims;

namespace estacionamientos.Controllers
{
    public class OcupacionController : Controller
    {
        private readonly AppDbContext _ctx;
        public OcupacionController(AppDbContext ctx) => _ctx = ctx;

        // ===========================
        // Helpers (combos / selects)
        // ===========================
        private async Task LoadSelects(int? plySel = null, int? plzSel = null, string? vehSel = null)
        {
            var playas = await _ctx.Playas.AsNoTracking()
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyCiu + " - " + p.PlyDir })
                .ToListAsync();

            List<object> plazas;
            if (plySel is null)
            {
                plazas = new List<object>();
            }
            else
            {
                plazas = await _ctx.Plazas
                    .Where(p => p.PlyID == plySel)
                    .OrderBy(p => p.PlzNum)
                    .Select(p => (object)new { p.PlzNum })
                    .ToListAsync();
            }

            var vehiculos = await _ctx.Vehiculos.AsNoTracking()
                .OrderBy(v => v.VehPtnt)
                .Select(v => v.VehPtnt)
                .ToListAsync();

            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", plySel);
            ViewBag.PlzNum = new SelectList(plazas, "PlzNum", "PlzNum", plzSel);
            ViewBag.VehPtnt = new SelectList(vehiculos, vehSel);
        }

        private Task<bool> PlazaExiste(int plyID, int plzNum)
            => _ctx.Plazas.AnyAsync(p => p.PlyID == plyID && p.PlzNum == plzNum);

        private Task<bool> VehiculoExiste(string pat)
            => _ctx.Vehiculos.AnyAsync(v => v.VehPtnt == pat);

        // ===========================
        // Lógica de cobro 
        // ===========================
        private async Task<CobroEgresoVM> CalcularCobro(
            int plyID, int plzNum, string vehPtnt, DateTime ocufFyhIni, DateTime ocufFyhFin)
        {
            var ocupacion = await _ctx.Ocupaciones
                .Include(o => o.Vehiculo).ThenInclude(v => v.Clasificacion)
                .Include(o => o.Plaza).ThenInclude(p => p.Playa)
                .FirstOrDefaultAsync(o =>
                    o.PlyID == plyID &&
                    o.PlzNum == plzNum &&
                    o.VehPtnt == vehPtnt &&
                    o.OcufFyhIni == ocufFyhIni);

            if (ocupacion == null)
                throw new InvalidOperationException("Ocupación no encontrada");

            var tiempoOcupacion = ocufFyhFin - ocufFyhIni;
            var horasOcupacion = (int)tiempoOcupacion.TotalHours;
            var minutosOcupacion = (int)tiempoOcupacion.TotalMinutes;

            // Traer servicios de tipo Estacionamiento habilitados
            var servicios = await _ctx.ServiciosProveidos
                .Include(sp => sp.Servicio)
                .Include(sp => sp.Tarifas.Where(t =>
                    t.ClasVehID == ocupacion.Vehiculo.ClasVehID &&
                    t.TasFecIni <= ocufFyhFin &&
                    (t.TasFecFin == null || t.TasFecFin >= ocufFyhIni)))
                .Where(sp => sp.PlyID == plyID &&
                            sp.SerProvHab &&
                            sp.Servicio.SerTipo == "Estacionamiento")
                .ToListAsync();

            // Cargar tarifas vigentes
            var tarifas = new List<(ServicioProveido sp, decimal monto, int minutos)>();
            foreach (var sp in servicios)
            {
                var tarifa = sp.Tarifas
                    .Where(t => t.ClasVehID == ocupacion.Vehiculo.ClasVehID &&
                                t.TasFecIni <= ocufFyhFin &&
                                (t.TasFecFin == null || t.TasFecFin >= ocufFyhIni))
                    .OrderByDescending(t => t.TasFecIni)
                    .FirstOrDefault();

                if (tarifa != null && sp.Servicio.SerDuracionMinutos.HasValue)
                {
                    tarifas.Add((sp, tarifa.TasMonto, sp.Servicio.SerDuracionMinutos.Value));
                }
            }

            // Buscar referencias a fracción (30min) y hora (60min)
            var fraccion = tarifas.FirstOrDefault(t => t.minutos == 30);
            var hora = tarifas.FirstOrDefault(t => t.minutos == 60);

            var serviciosAplicables = new List<ServicioCobroVM>();
            decimal totalCobro = 0;

            // ======================
            // Reglas especiales cortas
            // ======================
            if (minutosOcupacion <= 30 && fraccion.sp != null)
            {
                serviciosAplicables.Add(new ServicioCobroVM
                {
                    SerID = fraccion.sp.SerID,
                    SerNom = fraccion.sp.Servicio.SerNom,
                    TarifaVigente = fraccion.monto,
                    Cantidad = 1,
                    Subtotal = fraccion.monto,
                    EsEstacionamiento = true
                });
                totalCobro = fraccion.monto;
            }
            else if (minutosOcupacion <= 60 && hora.sp != null)
            {
                serviciosAplicables.Add(new ServicioCobroVM
                {
                    SerID = hora.sp.SerID,
                    SerNom = hora.sp.Servicio.SerNom,
                    TarifaVigente = hora.monto,
                    Cantidad = 1,
                    Subtotal = hora.monto,
                    EsEstacionamiento = true
                });
                totalCobro = hora.monto;
            }
            else
            {
                // ======================
                // Algoritmo greedy para el resto
                // ======================
                var tarifasLargas = tarifas
                    .Where(t => t.minutos > 60) // ignorar fracción y hora
                    .OrderByDescending(t => t.minutos)
                    .ToList();

                int minutosRestantes = minutosOcupacion;

                foreach (var (sp, monto, duracion) in tarifasLargas)
                {
                    int cantidad = minutosRestantes / duracion;
                    if (cantidad > 0)
                    {
                        serviciosAplicables.Add(new ServicioCobroVM
                        {
                            SerID = sp.SerID,
                            SerNom = sp.Servicio.SerNom,
                            TarifaVigente = monto,
                            Cantidad = cantidad,
                            Subtotal = monto * cantidad,
                            EsEstacionamiento = true
                        });
                        totalCobro += monto * cantidad;
                        minutosRestantes -= cantidad * duracion;
                    }
                }

                // Resolver sobrante con horas o fracción
                if (minutosRestantes > 0)
                {
                    if (minutosRestantes <= 30 && fraccion.sp != null)
                    {
                        serviciosAplicables.Add(new ServicioCobroVM
                        {
                            SerID = fraccion.sp.SerID,
                            SerNom = fraccion.sp.Servicio.SerNom,
                            TarifaVigente = fraccion.monto,
                            Cantidad = 1,
                            Subtotal = fraccion.monto,
                            EsEstacionamiento = true
                        });
                        totalCobro += fraccion.monto;
                    }
                    else if (hora.sp != null)
                    {
                        int cantHoras = (int)Math.Ceiling(minutosRestantes / 60.0);
                        serviciosAplicables.Add(new ServicioCobroVM
                        {
                            SerID = hora.sp.SerID,
                            SerNom = hora.sp.Servicio.SerNom,
                            TarifaVigente = hora.monto,
                            Cantidad = cantHoras,
                            Subtotal = hora.monto * cantHoras,
                            EsEstacionamiento = true
                        });
                        totalCobro += hora.monto * cantHoras;
                    }
                }
            }

            // Métodos de pago
            var metodosPago = await _ctx.AceptaMetodosPago
                .Include(amp => amp.MetodoPago)
                .Where(amp => amp.PlyID == plyID && amp.AmpHab)
                .Select(amp => new MetodoPagoVM
                {
                    MepID = amp.MepID,
                    MepNom = amp.MetodoPago.MepNom,
                    MepDesc = amp.MetodoPago.MepDesc
                })
                .ToListAsync();

            return new CobroEgresoVM
            {
                PlyID = plyID,
                PlzNum = plzNum,
                VehPtnt = vehPtnt,
                OcufFyhIni = DateTime.SpecifyKind(ocufFyhIni, DateTimeKind.Utc),
                OcufFyhFin = DateTime.SpecifyKind(ocufFyhFin, DateTimeKind.Utc),
                ClasVehID = ocupacion.Vehiculo.ClasVehID,
                ClasVehTipo = ocupacion.Vehiculo.Clasificacion?.ClasVehTipo ?? "",
                PlayaNombre = ocupacion.Plaza.Playa.PlyNom,
                TiempoOcupacion = tiempoOcupacion,
                HorasOcupacion = horasOcupacion,
                MinutosOcupacion = minutosOcupacion,
                ServiciosAplicables = serviciosAplicables,
                TotalCobro = totalCobro,
                MetodosPagoDisponibles = metodosPago
            };
        }


        private DateTime NormalizarFechaUTC(DateTime fecha)
        {
            if (fecha.Kind == DateTimeKind.Utc)
                return fecha;
            
            if (fecha.Kind == DateTimeKind.Local)
                return fecha.ToUniversalTime();
            
            // Si es Unspecified, asumir que es UTC
            return DateTime.SpecifyKind(fecha, DateTimeKind.Utc);
        }

        // ===========================
        // Listado
        // ===========================
        public async Task<IActionResult> Index()
        {   
            IQueryable<Ocupacion> query = _ctx.Ocupaciones
                .Include(o => o.Plaza)
                    .ThenInclude(p => p.Clasificaciones)
                        .ThenInclude(pc => pc.Clasificacion)
                .Include(o => o.Plaza).ThenInclude(p => p.Playa)
                .Include(o => o.Vehiculo).ThenInclude(v => v.Clasificacion)
                .Include(o => o.Pago)
                .AsNoTracking();

            // Filtro dinámico según rol
            if (User.IsInRole("Playero"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var turno = await _ctx.Turnos
                    .Where(t => t.PlaNU.ToString() == userId && t.TurFyhFin == null)
                    .Include(t => t.Playa)
                    .FirstOrDefaultAsync();

                if (turno == null)
                    return View("NoTurno");

                query = query.Where(o => o.PlyID == turno.PlyID);
            }

            // Ordenamiento: activos primero, luego por fecha de ingreso descendente
            query = query
                .OrderByDescending(o => o.OcufFyhFin == null ? 1 : 0) // activos primero
                .ThenByDescending(o => o.OcufFyhIni);

            var ocupaciones = await query.ToListAsync();

            return View(ocupaciones);
        }

        // ===========================
        // Acciones rápidas (ingreso/egreso)
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarIngreso(int plyID, int plzNum, string vehPtnt)
        {
            var ocup = new Ocupacion
            {
                PlyID = plyID,
                PlzNum = plzNum,
                VehPtnt = vehPtnt,
                OcufFyhIni = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                OcufFyhFin = null
            };

            _ctx.Ocupaciones.Add(ocup);

            var movimientoPlayero = new MovimientoPlayero{
                PlyID = plyID,
                PlaNU = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                TipoMov = TipoMovimiento.IngresoVehiculo,
                FechaMov = DateTime.UtcNow,
                VehPtnt = vehPtnt,
                PlzNum = plzNum,
            };

            _ctx.MovimientosPlayeros.Add(movimientoPlayero);

            await _ctx.SaveChangesAsync();

            TempData["Success"] = $"Vehículo {vehPtnt} ingresó a la plaza {plzNum}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarEgreso(int plyID, int plzNum, string vehPtnt)
        {
            // Debug logs
            System.Diagnostics.Debug.WriteLine($"RegistrarEgreso called: plyID={plyID}, plzNum={plzNum}, vehPtnt={vehPtnt}");
            
            var ocup = await _ctx.Ocupaciones
                .FirstOrDefaultAsync(o => o.PlyID == plyID && o.PlzNum == plzNum && o.VehPtnt == vehPtnt && o.OcufFyhFin == null);

            if (ocup == null)
            {
                System.Diagnostics.Debug.WriteLine("No se encontró ocupación activa");
                TempData["Error"] = $"No se encontró una ocupación activa para este vehículo. Parámetros: plyID={plyID}, plzNum={plzNum}, vehPtnt={vehPtnt}";
                return RedirectToAction(nameof(Index));
            }

            // Calcular el cobro antes de confirmar el egreso
            var fechaEgreso = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            var cobroVM = await CalcularCobro(plyID, plzNum, vehPtnt, ocup.OcufFyhIni, fechaEgreso);
            
            var movimientoPlayero = new MovimientoPlayero{
                PlyID = plyID,
                PlaNU = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                TipoMov = TipoMovimiento.EgresoVehiculo,
                FechaMov = DateTime.UtcNow,
                VehPtnt = vehPtnt,
                PlzNum = plzNum,
            };

            _ctx.MovimientosPlayeros.Add(movimientoPlayero);
            await _ctx.SaveChangesAsync();
            return View("CobroEgreso", cobroVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEgreso(CobroEgresoVM model)
        {
            // Normalizar fechas a UTC
            model.OcufFyhIni = NormalizarFechaUTC(model.OcufFyhIni);
            model.OcufFyhFin = NormalizarFechaUTC(model.OcufFyhFin);

            if (!ModelState.IsValid)
            {
                // Recargar el modelo con los datos originales
                var cobroVM = await CalcularCobro(model.PlyID, model.PlzNum, model.VehPtnt, 
                    model.OcufFyhIni, model.OcufFyhFin);
                cobroVM.MepID = model.MepID; // Mantener la selección del usuario
                return View("CobroEgreso", cobroVM);
            }

            // Buscar la ocupación activa (sin fecha de fin) en lugar de por fecha exacta
            var ocup = await _ctx.Ocupaciones
                .FirstOrDefaultAsync(o => o.PlyID == model.PlyID && o.PlzNum == model.PlzNum && o.VehPtnt == model.VehPtnt && o.OcufFyhFin == null);

            if (ocup == null)
            {
                TempData["Error"] = "No se encontró la ocupación especificada.";
                return RedirectToAction(nameof(Index));
            }

            await using var tx = await _ctx.Database.BeginTransactionAsync();

            try
            {
                // Crear el pago primero
                var proximoNumeroPago = await _ctx.Pagos
                    .Where(p => p.PlyID == model.PlyID)
                    .MaxAsync(p => (int?)p.PagNum) + 1 ?? 1;
                
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int plaNU = int.Parse(userId);

                var pago = new Pago
                {
                    PlyID = model.PlyID,
                    PlaNU = plaNU, 
                    PagNum = proximoNumeroPago,
                    MepID = model.MepID,
                    PagMonto = model.TotalCobro,
                    PagFyh = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                };

                _ctx.Pagos.Add(pago);
                await _ctx.SaveChangesAsync();

                // Actualizar la ocupación con la fecha de fin y asociar el pago
                ocup.OcufFyhFin = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                ocup.PagNum = pago.PagNum;
                _ctx.Ocupaciones.Update(ocup);
                await _ctx.SaveChangesAsync();

                // Si no quedan ocupaciones activas en esa plaza, marcarla como libre
                var sigueOcupada = await _ctx.Ocupaciones.AnyAsync(o =>
                    o.PlyID == model.PlyID && o.PlzNum == model.PlzNum && o.OcufFyhFin == null);

                if (!sigueOcupada)
                {
                    var plaza = await _ctx.Plazas.FindAsync(model.PlyID, model.PlzNum);
                    if (plaza != null && plaza.PlzOcupada)
                    {
                        plaza.PlzOcupada = false;
                        _ctx.Plazas.Update(plaza);
                        await _ctx.SaveChangesAsync();
                    }
                }

                await tx.CommitAsync();

                // Obtener el nombre del método de pago seleccionado
                var metodoPago = await _ctx.MetodosPago
                    .Where(mp => mp.MepID == model.MepID)
                    .Select(mp => mp.MepNom)
                    .FirstOrDefaultAsync();

                // Calcular el tiempo real de ocupación
                var tiempoReal = ocup.OcufFyhFin.Value - ocup.OcufFyhIni;
                var horasReales = (int)tiempoReal.TotalHours;
                var minutosReales = (int)tiempoReal.TotalMinutes % 60;

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = $"Error al procesar el egreso: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ===========================
        // Detalle
        // ===========================
        public async Task<IActionResult> Details(int plyID, int plzNum, string vehPtnt, DateTime ocufFyhIni)
        {
            var item = await _ctx.Ocupaciones
                .Include(o => o.Plaza).ThenInclude(p => p.Playa)
                .Include(o => o.Vehiculo)
                .Include(o => o.Pago)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.PlyID == plyID && o.PlzNum == plzNum && o.VehPtnt == vehPtnt && o.OcufFyhIni == ocufFyhIni);
            return item is null ? NotFound() : View(item);
        }

        [HttpGet]
        public async Task<IActionResult> DetallesJson(int plyID, int plzNum, string vehPtnt, DateTime ocufFyhIni)
        {
            // Normalizar la fecha de inicio a UTC
            var fechaInicioUTC = DateTime.SpecifyKind(ocufFyhIni, DateTimeKind.Utc);
            
            // Buscar la ocupación por los parámetros principales, permitiendo pequeñas diferencias de tiempo
            var ocupacion = await _ctx.Ocupaciones
                .Include(o => o.Plaza).ThenInclude(p => p.Playa)
                .Include(o => o.Vehiculo).ThenInclude(v => v.Clasificacion)
                .Include(o => o.Pago).ThenInclude(p => p.MetodoPago)
                .AsNoTracking()
                .Where(o => o.PlyID == plyID && o.PlzNum == plzNum && o.VehPtnt == vehPtnt)
                .Where(o => Math.Abs((o.OcufFyhIni - fechaInicioUTC).TotalMinutes) < 1) // Diferencia menor a 1 minuto
                .FirstOrDefaultAsync();

            if (ocupacion == null)
            {
                return Json(new { error = "Ocupación no encontrada" });
            }

            // para mostrar en el detalla las plazas anteriores en caso de que una ocupación haya sido cambiada de lugar
            var historialPlazas = await _ctx.MovimientosPlayeros
                .Where(
                    m => m.PlyID == plyID && 
                    m.VehPtnt == vehPtnt && 
                    m.FechaMov > ocufFyhIni && 
                    (m.TipoMov == TipoMovimiento.IngresoVehiculo || m.TipoMov == TipoMovimiento.ReubicacionVehiculo))
                .OrderBy(m => m.FechaMov)
                .Select(m => m.PlzNum)
                .ToListAsync();

            var resultado = new
            {
                plyID = ocupacion.PlyID,
                plzNum = ocupacion.PlzNum,
                vehPtnt = ocupacion.VehPtnt,
                ocufFyhIni = ocupacion.OcufFyhIni,
                ocufFyhFin = ocupacion.OcufFyhFin,
                clasificacionVehiculo = ocupacion.Vehiculo?.Clasificacion?.ClasVehTipo ?? "No especificada",
                playaNombre = ocupacion.Plaza?.Playa?.PlyNom ?? "No especificada",
                pago = (ocupacion.OcufFyhFin != null && ocupacion.Pago != null) 
                    ? new
                    {
                        pagNum = ocupacion.Pago.PagNum,
                        metodoPago = ocupacion.Pago.MetodoPago?.MepNom ?? "No especificado",
                        pagMonto = ocupacion.Pago.PagMonto,
                        pagFyh = ocupacion.Pago.PagFyh
                    } 
                    : null,
                historialPlazas = historialPlazas ?? null
            };
            return Json(resultado);
        }

        // ===========================
        // API para la grilla de plazas
        // ===========================
        [HttpGet]
        public async Task<JsonResult> GetPlazasDisponibles(int plyID, int? clasVehID, string? techada = "all")
        {
            // Traemos TODAS las plazas y calculamos flags para que el front pueda pintar/filtrar
            var baseQuery = _ctx.Plazas.AsNoTracking()
                .Where(p => p.PlyID == plyID)
                .Select(p => new
                {
                    p.PlyID,
                    plzNum = p.PlzNum,
                    nombre = p.PlzNombre,
                    piso = p.Piso,           // para el combo de pisos
                    hab = p.PlzHab,
                    techada = p.PlzTecho,
                    compatible = (clasVehID == null || 
                                p.Clasificaciones.Any(pc => pc.ClasVehID == clasVehID)),

                    ocupada = _ctx.Ocupaciones.Any(o => o.PlyID == p.PlyID
                                                        && o.PlzNum == p.PlzNum
                                                        && o.OcufFyhFin == null)
                });

            // Filtro de techada si vino
            if (techada == "true") baseQuery = baseQuery.Where(x => x.techada);
            if (techada == "false") baseQuery = baseQuery.Where(x => !x.techada);
            // "all" o null → sin filtro

            var data = await baseQuery
                .OrderBy(x => x.piso).ThenBy(x => x.plzNum)
                .ToListAsync();

            // Normalizamos nombre y ocultamos "techada" (la vista no lo usa)
            var payload = data.Select(x => new
            {
                x.plzNum,
                nombre = string.IsNullOrWhiteSpace(x.nombre) ? $"P{x.plzNum}" : x.nombre,
                x.piso,
                x.hab,
                x.compatible,
                x.ocupada
            });

            return Json(payload);
        }

        // ===========================
        // Alta por formulario (vista Create)
        // ===========================
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
                    TempData["Error"] = "Debe tener un turno activo para registrar ingresos.";
                    return RedirectToAction(nameof(Index));
                }

                var playaNombre = await _ctx.Playas
                    .Where(p => p.PlyID == turno.PlyID)
                    .Select(p => p.PlyNom)
                    .FirstOrDefaultAsync();

                ViewBag.PlayaNombre = playaNombre;

                await LoadSelects(turno.PlyID);

                ViewBag.Clasificaciones = new SelectList(
                    await _ctx.ClasificacionesVehiculo
                        .OrderBy(c => c.ClasVehTipo)
                        .ToListAsync(),
                    "ClasVehID", "ClasVehTipo"
                );

                return View(new Ocupacion
                {
                    OcufFyhIni = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                    PlyID = turno.PlyID
                });
            }

            await LoadSelects();
            ViewBag.Clasificaciones = new SelectList(
                await _ctx.ClasificacionesVehiculo
                    .OrderBy(c => c.ClasVehTipo)
                    .ToListAsync(),
                "ClasVehID", "ClasVehTipo"
            );
            return View(new Ocupacion { OcufFyhIni = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc) });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ocupacion model, int ClasVehID)
        {
            // Revalidación de modelo y combos
            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.PlzNum, model.VehPtnt);
                ViewBag.Clasificaciones = new SelectList(
                    await _ctx.ClasificacionesVehiculo
                        .OrderBy(c => c.ClasVehTipo)
                        .ToListAsync(),
                    "ClasVehID", "ClasVehTipo", ClasVehID
                );
                ViewBag.PlayaNombre = await _ctx.Playas
                    .Where(p => p.PlyID == model.PlyID)
                    .Select(p => p.PlyNom)
                    .FirstOrDefaultAsync();

                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.VehPtnt))
            {
                TempData["Error"] = "Debe ingresar la patente del vehículo.";
                return RedirectToAction(nameof(Create));
            }

            if (ClasVehID == 0)
            {
                TempData["Error"] = "Debe seleccionar una clasificación válida.";
                return RedirectToAction(nameof(Index));
            }

            var plazaValida = await _ctx.Plazas
                .AnyAsync(p =>
                    p.PlyID == model.PlyID &&
                    p.PlzNum == model.PlzNum!.Value &&
                    p.PlzHab == true &&
                    p.Clasificaciones.Any(pc => pc.ClasVehID == ClasVehID));


            if (!plazaValida)
            {
                TempData["Error"] = "La plaza seleccionada no es válida para esta clasificación.";
                return RedirectToAction(nameof(Index));
            }

            var yaOcupada = await _ctx.Ocupaciones.AnyAsync(o =>
                o.PlyID == model.PlyID &&
                o.PlzNum == model.PlzNum!.Value &&
                o.OcufFyhFin == null);

            if (yaOcupada)
            {
                TempData["Error"] = "La plaza ya está ocupada.";
                return RedirectToAction(nameof(Index));
            }

            if (model.PlzNum.HasValue && !await PlazaExiste(model.PlyID, model.PlzNum.Value))
            {
                TempData["Error"] = "La plaza no existe en la playa seleccionada.";
                return RedirectToAction(nameof(Index));
            }

            var patenteOcupada = await _ctx.Ocupaciones.AnyAsync(o =>
                o.PlyID == model.PlyID &&           // si querés limitarlo a la misma playa
                o.VehPtnt == model.VehPtnt &&
                o.OcufFyhFin == null);

            if (patenteOcupada)
            {
                ModelState.AddModelError("VehPtnt", $"El vehículo con patente {model.VehPtnt} ya tiene un ingreso en curso.");

                // 🔄 Recargar combos igual que arriba
                await LoadSelects(model.PlyID, model.PlzNum, model.VehPtnt);
                ViewBag.Clasificaciones = new SelectList(
                    await _ctx.ClasificacionesVehiculo
                        .OrderBy(c => c.ClasVehTipo)
                        .ToListAsync(),
                    "ClasVehID", "ClasVehTipo", ClasVehID
                );
                ViewBag.PlayaNombre = await _ctx.Playas
                    .Where(p => p.PlyID == model.PlyID)
                    .Select(p => p.PlyNom)
                    .FirstOrDefaultAsync();

                return View(model);  // ⬅️ vuelve al formulario mostrando el error debajo del campo
            }



            // Alta automática de vehículo si no existe / actualizar clasif si corresponde
            var vehiculo = await _ctx.Vehiculos.FirstOrDefaultAsync(v => v.VehPtnt == model.VehPtnt);
            if (vehiculo == null)
            {
                vehiculo = new Vehiculo
                {
                    VehPtnt = model.VehPtnt,
                    ClasVehID = ClasVehID,
                    VehMarc = "Desconocida"
                };
                _ctx.Vehiculos.Add(vehiculo);
                await _ctx.SaveChangesAsync();
            }
            else if (vehiculo.ClasVehID == 0 && ClasVehID != 0)
            {
                vehiculo.ClasVehID = ClasVehID;
                _ctx.Update(vehiculo);
                await _ctx.SaveChangesAsync();
            }

            // Guardar ocupación + marcar plaza ocupada (transacción)
            await using var tx = await _ctx.Database.BeginTransactionAsync();

            model.OcufFyhIni = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            model.OcufFyhFin = null;
            _ctx.Ocupaciones.Add(model);

            // registrar el movimiento del playero
            
            var movimientoPlayero = new MovimientoPlayero{
                PlyID = model.PlyID,
                PlaNU = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                TipoMov = TipoMovimiento.IngresoVehiculo,
                FechaMov = DateTime.UtcNow,
                VehPtnt = model.VehPtnt,
                PlzNum = model.PlzNum,
            };

            _ctx.MovimientosPlayeros.Add(movimientoPlayero);
            await _ctx.SaveChangesAsync();

            var plaza = await _ctx.Plazas.FindAsync(model.PlyID, model.PlzNum);
            if (plaza != null && !plaza.PlzOcupada)
            {
                plaza.PlzOcupada = true;
                _ctx.Plazas.Update(plaza);
                await _ctx.SaveChangesAsync();
            }

            await tx.CommitAsync();

            TempData["Success"] = $"Vehículo {model.VehPtnt} ingresó a la plaza {model.PlzNum}.";
            return RedirectToAction(nameof(Index));
        }

        // ===========================
        // Editar / Eliminar
        // ===========================
        public async Task<IActionResult> Edit(int plyID, int plzNum, string vehPtnt, DateTime ocufFyhIni)
        {
            var item = await _ctx.Ocupaciones.FindAsync(plyID, plzNum, vehPtnt, ocufFyhIni);
            if (item is null) return NotFound();

            await LoadSelects(item.PlyID, item.PlzNum, item.VehPtnt);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int plzNum, string vehPtnt, DateTime ocufFyhIni, Ocupacion model)
        {
            if (plyID != model.PlyID || plzNum != model.PlzNum || vehPtnt != model.VehPtnt || ocufFyhIni != model.OcufFyhIni)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.PlzNum, model.VehPtnt);
                return View(model);
            }

            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int plyID, int plzNum, string vehPtnt, DateTime ocufFyhIni)
        {
            var item = await _ctx.Ocupaciones
                .Include(o => o.Plaza).ThenInclude(p => p.Playa)
                .Include(o => o.Vehiculo)
                .Include(o => o.Pago)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.PlyID == plyID && o.PlzNum == plzNum && o.VehPtnt == vehPtnt && o.OcufFyhIni == ocufFyhIni);

            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int plzNum, string vehPtnt, DateTime ocufFyhIni)
        {
            var item = await _ctx.Ocupaciones.FindAsync(plyID, plzNum, vehPtnt, ocufFyhIni);
            if (item is null) return NotFound();

            _ctx.Ocupaciones.Remove(item);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]

        public async Task<IActionResult> ReubicarVehiculo(int plyID, int plzNum, string vehPtnt)
        {   
            await PrepararVistaReubicar(plyID, plzNum, vehPtnt, string.Empty);
            return View("ReubicarVehiculo");
        }

        // POST: /Ocupacion/ReubicarVehiculo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReubicarVehiculo(int plyID, int plzNum, string vehPtnt, int nuevaPlazaId)
        {
            try
            {
                Console.WriteLine($"Modificando ocupación estacionamiento {plyID}, patente: {vehPtnt} (plaza {plzNum} -> plaza {nuevaPlazaId})");
                var ocupacionActual = await _ctx.Ocupaciones
                    .Include(o => o.Plaza)
                    .FirstOrDefaultAsync(o => o.PlyID == plyID && o.PlzNum == plzNum && o.VehPtnt == vehPtnt && o.OcufFyhFin == null);

                if (ocupacionActual == null)
                {
                    Console.WriteLine("Error: No se encontró la ocupación actual.");
                    await PrepararVistaReubicar(plyID, plzNum, vehPtnt, "Error al buscar la ocupación actual");
                    return View("ReubicarVehiculo");
                }

                var nuevaPlaza = await _ctx.Plazas
                    .FirstOrDefaultAsync(p => p.PlyID == plyID && p.PlzNum == nuevaPlazaId);

                if (nuevaPlaza == null || !nuevaPlaza.PlzHab)
                {
                    Console.WriteLine("Error: La plaza seleccionada no está disponible.");
                    await PrepararVistaReubicar(plyID, plzNum, vehPtnt, "La plaza seleccionada no está disponible.", ocupacionActual);
                    return View("ReubicarVehiculo");
                }

                if(ocupacionActual.OcufFyhFin != null || ocupacionActual.PagNum != null){
                    Console.WriteLine("Error: No se puede modificar una ocupación ya terminada");
                    Console.WriteLine($"ocupacionActual.OcufFyhFin: {ocupacionActual.OcufFyhFin}");
                    Console.WriteLine($"ocupacionActual.PagNum: {ocupacionActual.PagNum}");
                    await PrepararVistaReubicar(plyID, plzNum, vehPtnt, "No se puede modificar una ocupación ya terminada");
                    return View("ReubicarVehiculo");
                }
                // Liberar la plaza
                ocupacionActual.Plaza.PlzHab = true;

                // Borrar ocupación anterior (para insertar una nueva con misma clave pero distinta plaza)
                _ctx.Ocupaciones.Remove(ocupacionActual);

                //Crear una nueva ocupación
                var nuevaOcupacion = new Ocupacion{
                    PlyID = plyID,
                    PlzNum = nuevaPlaza.PlzNum,
                    VehPtnt = ocupacionActual.VehPtnt,
                    OcufFyhIni = ocupacionActual.OcufFyhIni,
                    OcuLlavDej = ocupacionActual.OcuLlavDej,
                    PagNum = ocupacionActual.PagNum,
                };

                nuevaPlaza.PlzHab = false;
                
                _ctx.Ocupaciones.Add(nuevaOcupacion);

                //registrar el movimiento del playero
                var usuNu = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var movimientoPlayero = new MovimientoPlayero{
                    PlyID = nuevaOcupacion.PlyID,
                    PlaNU = usuNu,
                    TipoMov = TipoMovimiento.ReubicacionVehiculo,
                    FechaMov = DateTime.UtcNow,
                    VehPtnt = nuevaOcupacion.VehPtnt,
                    PlzNum = nuevaOcupacion.PlzNum,
                };

                _ctx.MovimientosPlayeros.Add(movimientoPlayero);

                await _ctx.SaveChangesAsync();

                TempData["Success"] = $"Plaza modificada para {ocupacionActual?.Vehiculo?.Clasificacion.ClasVehTipo ?? "vehículo"} {nuevaOcupacion.VehPtnt}";
                Console.WriteLine("Plaza cambiada correctamente");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado: {ex.Message}");
                await PrepararVistaReubicar(plyID, plzNum, vehPtnt, "Ocurrió un error inesperado. Intente nuevamente.");
                return View("ReubicarVehiculo");
            }
        }

        // Método privado para preparar datos de la vista
        private async Task PrepararVistaReubicar(int plyID, int plzNum, string vehPtnt, string errorMensaje, Ocupacion? ocupacion = null)
        {
            var ocupacionActual = ocupacion ?? await _ctx.Ocupaciones
                .Where(o => o.PlyID == plyID && o.PlzNum == plzNum && o.VehPtnt == vehPtnt && o.OcufFyhFin == null)
                .Include(o => o.Plaza)
                    .ThenInclude(p => p.Clasificaciones)
                        .ThenInclude(pc => pc.Clasificacion)
                .Include(o => o.Plaza)
                    .ThenInclude(p => p.Playa)
                .Include(o => o.Vehiculo)
                    .ThenInclude(v => v.Clasificacion)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            var plazasValidas = await _ctx.PlazasClasificaciones
                .Where(plz => plz.PlyID == plyID)
                .Include(plz => plz.Plaza)
                    .ThenInclude(plz => plz.Ocupaciones.Where(o => o.OcufFyhFin == null))
                .Include(plz => plz.Clasificacion)
                .AsNoTracking()
                .ToListAsync();

            var plazasUnicas = plazasValidas
                .GroupBy(pc => pc.Plaza.PlzNum)
                .Select(g => g.First())
                .ToList();
                

            ViewBag.ListaPlazas = plazasUnicas;
            ViewBag.ErrorMensaje = errorMensaje;

            // Enviamos la ocupación al ViewData para la vista
            ViewBag.OcupacionActual = ocupacionActual;
        }
    }
}
