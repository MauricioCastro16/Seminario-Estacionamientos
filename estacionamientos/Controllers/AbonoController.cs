using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.ViewModels;
using estacionamientos.ViewModels.SelectOptions;
using estacionamientos.Helpers;
using System.Security.Claims;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System.Text.Json; 




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
            List<Abono> abonos;
            
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
                    .Include(a => a.Periodos)
                    .Where(a => a.PlyID == turno.PlyID) // solo abonos de la playa del turno
                    .AsNoTracking();

                abonos = await q.ToListAsync();
            }
            else
            {
                // Si no es playero → muestra todos los abonos
                var qAll = _ctx.Abonos
                    .Include(a => a.Plaza).ThenInclude(p => p.Playa)
                    .Include(a => a.Abonado)
                    .Include(a => a.Pago).ThenInclude(p => p.MetodoPago)
                    .Include(a => a.Vehiculos).ThenInclude(v => v.Vehiculo).ThenInclude(v => v.Clasificacion)
                    .Include(a => a.Periodos)
                    .AsNoTracking();

                abonos = await qAll.ToListAsync();
            }

            // 🔹 Recalcular el estado de cada abono dinámicamente
            var hoy = DateTime.Now;
            foreach (var abono in abonos)
            {
                var estadoTexto = CalcularEstadoTexto(abono, hoy);
                
                // Actualizar el estado en el objeto (sin guardar en BD)
                abono.EstadoPago = estadoTexto switch
                {
                    "Al Día" => EstadoPago.Activo,
                    "Pendiente" => EstadoPago.Pendiente,
                    "Finalizado" => EstadoPago.Finalizado,
                    "Cancelado" => EstadoPago.Cancelado,
                    _ => EstadoPago.Pendiente
                };
            }
            
            Console.WriteLine($"🔹 Index - Recalculados {abonos.Count} abonos dinámicamente");
            
            // 🔹 Ordenar por estado: Pendiente, Al día, Finalizado, Cancelado
            abonos = abonos.OrderBy(a => a.EstadoPago switch
            {
                EstadoPago.Pendiente => 1,
                EstadoPago.Activo => 2,
                EstadoPago.Finalizado => 3,
                EstadoPago.Cancelado => 4,
                _ => 5
            }).ToList();
            
            return View(abonos);
        }



        public async Task<IActionResult> Create(string abonado = null, string dni = null, string vehiculos = null, int? plyID = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var model = new AbonoCreateVM { AboFyhIni = DateTime.UtcNow };

            // 🔹 Precargar datos si vienen de extensión
            if (!string.IsNullOrEmpty(abonado) && !string.IsNullOrEmpty(dni))
            {
                model.AboNom = abonado;
                model.AboDNI = dni;
                
                // Precargar vehículos si vienen de extensión
                if (!string.IsNullOrEmpty(vehiculos))
                {
                    try
                    {
                        var vehiculosData = JsonSerializer.Deserialize<List<VehiculoInfo>>(vehiculos);
                        model.Vehiculos = vehiculosData.Select(v => new VehiculoVM
                        {
                            VehPtnt = v.patente
                        }).ToList();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deserializando vehículos: {ex.Message}");
                        model.Vehiculos = new List<VehiculoVM>();
                    }
                }
                else
                {
                    model.Vehiculos = new List<VehiculoVM>();
                }
            }
            else
            {
                model.Vehiculos = new List<VehiculoVM>();
            }

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

                model.PlyID = turno.PlyID;
                return View(model);
            }

            // Si se especifica plyID (viene de extensión), usar ese
            if (plyID.HasValue)
            {
                model.PlyID = plyID.Value;
                await LoadSelects(plyID.Value);
            }
            else
            {
                await LoadSelects();
            }

            // 🔹 Cargar clasificaciones también aquí
            ViewBag.ClasVehID = new SelectList(
                await _ctx.ClasificacionesVehiculo
                    .OrderBy(c => c.ClasVehTipo)   // 👈 usar ClasVehTipo
                    .ToListAsync(),
                "ClasVehID", "ClasVehTipo"        // 👈 usar ClasVehTipo
            );

            return View(model);
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

            // ✅ Verificar disponibilidad de plaza para las fechas seleccionadas
            if (model.SelectedPlzNum.HasValue && model.AboFyhIni != default && model.AboFyhFin.HasValue)
            {
                var disponible = await _ctx.Abonos
                    .Where(a => a.PlyID == model.PlyID && a.PlzNum == model.SelectedPlzNum && a.EstadoPago != EstadoPago.Cancelado)
                    .AllAsync(a => a.AboFyhFin < model.AboFyhIni);

                if (!disponible)
                {
                    TempData["Error"] = "La plaza seleccionada está ocupada en ese período. Seleccione otra o elija una fecha posterior.";
                    await LoadSelects(model.PlyID);
                    return View(model);
                }
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
            // Normalizar la fecha para PostgreSQL
            var fechaUtc = DateTime.SpecifyKind(aboFyhIni, DateTimeKind.Utc);
            
            var item = await _ctx.Abonos
                .Include(a => a.Abonado)
                .Include(a => a.Plaza)
                .Include(a => a.Pago)
                    .ThenInclude(p => p.MetodoPago)
                .Include(a => a.Vehiculos)
                    .ThenInclude(v => v.Vehiculo)
                        .ThenInclude(v => v.Clasificacion)
                .Include(a => a.Periodos)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => 
                    a.PlyID == plyID && 
                    a.PlzNum == plzNum && 
                    EF.Functions.DateDiffSecond(a.AboFyhIni, fechaUtc) == 0);

                            
            if (item is null) return NotFound();
            
            // 🔹 Recalcular el estado del abono dinámicamente para mostrar siempre el estado correcto
            var hoy = DateTime.Now;
            var estadoTexto = CalcularEstadoTexto(item, hoy);
            
            // Actualizar el estado en el objeto (sin guardar en BD)
            item.EstadoPago = estadoTexto switch
            {
                "Al Día" => EstadoPago.Activo,
                "Pendiente" => EstadoPago.Pendiente,
                "Finalizado" => EstadoPago.Finalizado,
                "Cancelado" => EstadoPago.Cancelado,
                _ => EstadoPago.Pendiente
            };
            
            Console.WriteLine($"🔹 Details - Estado recalculado dinámicamente: {estadoTexto} -> {item.EstadoPago}");
            
            return View(item);
        }

        // ✅ Redirección desde "Extender abono" a la vista Create con datos precargados
            [HttpGet]
            public async Task<IActionResult> ExtenderRedirect(int plyID, int plzNum, DateTime aboFyhIni)
            {
                var abono = await _ctx.Abonos
                    .Include(a => a.Abonado)
                    .Include(a => a.Vehiculos).ThenInclude(v => v.Vehiculo)
                    .FirstOrDefaultAsync(a => a.PlyID == plyID && a.PlzNum == plzNum && a.AboFyhIni == aboFyhIni);

                if (abono == null) return NotFound();

                // Serializamos los vehículos asociados
                var vehiculosJson = JsonSerializer.Serialize(
                    abono.Vehiculos.Select(v => new { patente = v.VehPtnt }).ToList()
                );

                // Redirigimos al Create con los datos precargados
                return RedirectToAction("Create", new
                {
                    abonado = abono.Abonado.AboNom,
                    dni = abono.Abonado.AboDNI,
                    vehiculos = vehiculosJson,
                    plyID = abono.PlyID
                });
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
                Console.WriteLine($"ConfirmarPago - Iniciando con datos: PlyID={model.PlyID}, PlzNum={model.SelectedPlzNum}, Vehículos={model.Vehiculos?.Count ?? 0}");
                Console.WriteLine($"ConfirmarPago - Datos del modelo: SerID={model.SerID}, ClasVehID={model.ClasVehID}, Periodos={model.Periodos}");
                Console.WriteLine($"ConfirmarPago - Abonado: DNI={model.AboDNI}, Nombre={model.AboNom}");
                Console.WriteLine($"ConfirmarPago - Pago: MepID={model.MepID}, OpcionPago={model.OpcionPago}, CantidadPeriodosPagar={model.CantidadPeriodosPagar}, MontoPagar={model.MontoPagar}");
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    var fieldErrors = ModelState.Where(x => x.Value.Errors.Count > 0)
                        .Select(x => $"{x.Key}: {string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))}");
                    
                    Console.WriteLine($"ModelState inválido - Errores generales: {string.Join(", ", errors)}");
                    Console.WriteLine($"ModelState inválido - Errores por campo: {string.Join("; ", fieldErrors)}");
                    
                    return Json(new { 
                        success = false, 
                        message = "Datos inválidos", 
                        errors = fieldErrors.ToList(),
                        details = errors.ToList()
                    });
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

                // 💡 Validación: evitar pagos con monto inválido o muy bajo
                if (model.MontoPagar <= 0 || model.MontoPagar < 100)
                {
                    Console.WriteLine($"[AVISO] Pago descartado: monto inválido ({model.MontoPagar}). Se ajustará a 0 para evitar registros incorrectos.");
                    model.MontoPagar = 0;
                }

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
                Console.WriteLine($"Creando abono: PlyID={model.PlyID}, PlzNum={model.SelectedPlzNum}, AboFyhIni={model.AboFyhIni}");
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
                Console.WriteLine("Abono agregado al contexto, guardando cambios...");
                await _ctx.SaveChangesAsync();
                Console.WriteLine("Abono guardado exitosamente");

                // 5. Crear o verificar vehículos y asociarlos al abono
                Console.WriteLine($"Procesando {model.Vehiculos?.Count ?? 0} vehículos");
                foreach (var vehiculoVM in model.Vehiculos)
                {
                    Console.WriteLine($"Procesando vehículo: {vehiculoVM.VehPtnt}");
                    
                    // Verificar si el vehículo existe
                    var vehiculo = await _ctx.Vehiculos.FindAsync(vehiculoVM.VehPtnt);
                    if (vehiculo == null)
                    {
                        Console.WriteLine($"Creando nuevo vehículo: {vehiculoVM.VehPtnt}");
                        // Crear nuevo vehículo con la clasificación seleccionada
                        vehiculo = new Vehiculo
                        {
                            VehPtnt = vehiculoVM.VehPtnt,
                            ClasVehID = model.ClasVehID
                        };
                        _ctx.Vehiculos.Add(vehiculo);
                        await _ctx.SaveChangesAsync();
                        Console.WriteLine($"Vehículo {vehiculoVM.VehPtnt} creado exitosamente");
                    }
                    else
                    {
                        Console.WriteLine($"Vehículo {vehiculoVM.VehPtnt} ya existe");
                    }

                    // Verificar si ya existe la asociación VehiculoAbonado
                    Console.WriteLine($"Verificando asociación VehiculoAbonado para {vehiculoVM.VehPtnt}");
                    var vehiculoAbonadoExistente = await _ctx.VehiculosAbonados
                        .FirstOrDefaultAsync(va => va.PlyID == model.PlyID && 
                                                   va.PlzNum == model.SelectedPlzNum && 
                                                   va.AboFyhIni == model.AboFyhIni && 
                                                   va.VehPtnt == vehiculoVM.VehPtnt);
                    
                    if (vehiculoAbonadoExistente == null)
                    {
                        Console.WriteLine($"Creando asociación VehiculoAbonado para {vehiculoVM.VehPtnt}");
                        // Asociar vehículo al abono solo si no existe
                        var vehiculoAbonado = new VehiculoAbonado
                        {
                            PlyID = model.PlyID,
                            PlzNum = model.SelectedPlzNum,
                            AboFyhIni = model.AboFyhIni,
                            VehPtnt = vehiculoVM.VehPtnt
                        };
                        _ctx.VehiculosAbonados.Add(vehiculoAbonado);
                        Console.WriteLine($"Asociación VehiculoAbonado para {vehiculoVM.VehPtnt} agregada al contexto");
                    }
                    else
                    {
                        Console.WriteLine($"Asociación VehiculoAbonado para {vehiculoVM.VehPtnt} ya existe");
                    }
                }

                // 6. Crear períodos del abono
                Console.WriteLine($"Creando períodos del abono: {model.CantidadPeriodosPagar} períodos pagados de {model.Periodos} totales");
                await CrearPeriodosAbono(model, abono);
                Console.WriteLine($"Total períodos creados: {_ctx.PeriodosAbono.Count()}");

                Console.WriteLine("Guardando cambios finales...");
                await _ctx.SaveChangesAsync();
                Console.WriteLine("Cambios guardados, confirmando transacción...");
                await transaction.CommitAsync();
                Console.WriteLine("Transacción confirmada exitosamente");

                return Json(new { success = true, message = "Abono registrado exitosamente" });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                Console.WriteLine("==== ERROR EN CONFIRMAR PAGO ====");
                Console.WriteLine(msg);
                Console.WriteLine(ex.StackTrace);

                return Json(new { success = false, message = msg });
            }

        }

        // API: verificar disponibilidad de plaza en fechas específicas
        [HttpGet]
        public async Task<IActionResult> VerificarDisponibilidadPlaza(int plyId, int plzNum, DateTime fechaIni, DateTime fechaFin, DateTime? excluirAbono = null)
        {
            try
            {
                // Convertir fechas a UTC para PostgreSQL
                var fechaIniUTC = DateTime.SpecifyKind(fechaIni, DateTimeKind.Utc);
                var fechaFinUTC = DateTime.SpecifyKind(fechaFin, DateTimeKind.Utc);
                
                Console.WriteLine($"Verificando disponibilidad: PlyID={plyId}, PlzNum={plzNum}, FechaIni={fechaIniUTC:yyyy-MM-dd HH:mm:ss}, FechaFin={fechaFinUTC:yyyy-MM-dd HH:mm:ss}");

                // Buscar abonos existentes en la plaza que se superpongan con las fechas
                var query = _ctx.Abonos
                    .Where(a => a.PlyID == plyId && 
                               a.PlzNum == plzNum && 
                               a.EstadoPago != EstadoPago.Cancelado &&
                               ((a.AboFyhIni <= fechaFinUTC && (a.AboFyhFin == null || a.AboFyhFin >= fechaIniUTC))));
                
                // Excluir el abono especificado si se proporciona
                if (excluirAbono.HasValue)
                {
                    var excluirAbonoUTC = DateTime.SpecifyKind(excluirAbono.Value, DateTimeKind.Utc);
                    query = query.Where(a => a.AboFyhIni != excluirAbonoUTC);
                }
                
                var abonosExistentes = await query
                    .Select(a => new { 
                        a.AboFyhIni, 
                        a.AboFyhFin, 
                        a.Abonado.AboNom,
                        a.EstadoPago
                    })
                    .ToListAsync();

                if (abonosExistentes.Any())
                {
                    var abonoExistente = abonosExistentes.First();
                    var fechaFinExistente = abonoExistente.AboFyhFin?.ToString("dd/MM/yyyy") ?? "Sin fecha de fin";
                    
                    return Json(new { 
                        disponible = false, 
                        mensaje = $"La plaza {plzNum} está ocupada por el abonado {abonoExistente.AboNom} desde {abonoExistente.AboFyhIni:dd/MM/yyyy} hasta {fechaFinExistente}. Puede programar un abono para después de esa fecha.",
                        fechaFinOcupacion = abonoExistente.AboFyhFin,
                        estadoAbono = abonoExistente.EstadoPago.ToString()
                    });
                }

                return Json(new { 
                    disponible = true, 
                    mensaje = "Plaza disponible para las fechas seleccionadas" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    disponible = false, 
                    mensaje = $"Error verificando disponibilidad: {ex.Message}" 
                });
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

        [HttpPost]
        public async Task<IActionResult> PopulateExistingAbonosPeriods()
        {
            try
            {
                var script = new Scripts.PopulateExistingAbonosPeriods(_ctx);
                await script.PopulatePeriodsForExistingAbonos();
                
                TempData["Success"] = "Períodos creados exitosamente para abonos existentes.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creando períodos: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task CrearPeriodosAbono(ConfirmarPagoAbonoVM model, Abono abono)
        {
            var servicio = await _ctx.Servicios.AsNoTracking().FirstOrDefaultAsync(s => s.SerID == model.SerID);
            int diasPorPeriodo = 0;
            if (servicio?.SerDuracionMinutos != null)
                diasPorPeriodo = (int)Math.Ceiling(servicio.SerDuracionMinutos.Value / 1440m);
            else
                diasPorPeriodo = model.SerID switch { 7 => 1, 8 => 7, 9 => 30, _ => 1 };

            // Tarifa vigente
            var tarifa = await _ctx.TarifasServicio
                .Where(t => t.PlyID == model.PlyID
                        && t.SerID == model.SerID
                        && t.ClasVehID == model.ClasVehID
                        && (t.TasFecFin == null || t.TasFecFin >= DateTime.UtcNow))
                .OrderByDescending(t => t.TasFecIni)
                .FirstOrDefaultAsync();

            var montoPorPeriodo = tarifa?.TasMonto ?? 0m;
            // 💡 Validación adicional: si no hay tarifa válida o el monto es 0, mostrar aviso
            if (montoPorPeriodo <= 0)
            {
                Console.WriteLine($"[AVISO] Tarifa no encontrada o monto inválido ({montoPorPeriodo}). No se generarán pagos.");
            }


            for (int i = 1; i <= model.Periodos; i++)
            {
                var fechaInicio = abono.AboFyhIni.AddDays((i - 1) * diasPorPeriodo);
                var fechaFin = fechaInicio.AddDays(diasPorPeriodo);

                var periodo = new PeriodoAbono
                {
                    PlyID = abono.PlyID,
                    PlzNum = abono.PlzNum,
                    AboFyhIni = DateTime.SpecifyKind(abono.AboFyhIni, DateTimeKind.Utc),
                    PeriodoNumero = i,
                    PeriodoFechaInicio = DateTime.SpecifyKind(fechaInicio, DateTimeKind.Utc),
                    PeriodoFechaFin = DateTime.SpecifyKind(fechaFin, DateTimeKind.Utc),
                    PeriodoMonto = montoPorPeriodo,
                    PeriodoPagado = i <= model.CantidadPeriodosPagar,
                    PeriodoFechaPago = i <= model.CantidadPeriodosPagar ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc) : null
                };

                // 🔹 Si el período está pagado, generamos un Pago vinculado
                if (periodo.PeriodoPagado)
                {
                    // Evitar crear pagos con monto nulo o erróneo
                    if (montoPorPeriodo <= 0)
                    {
                        Console.WriteLine($"[AVISO] No se generó pago para el período {i} porque el monto ({montoPorPeriodo}) es inválido.");
                        continue;
                    }

                    var nextPagNum = (_ctx.Pagos
                        .Where(p => p.PlyID == model.PlyID)
                        .Select(p => (int?)p.PagNum)
                        .Max() ?? 0) + 1;

                    var pagoPeriodo = new Pago
                    {
                        PlyID = model.PlyID,
                        PagNum = nextPagNum,
                        MepID = model.MepID,
                        PagMonto = montoPorPeriodo,
                        PagFyh = DateTime.UtcNow
                    };

                    _ctx.Pagos.Add(pagoPeriodo);
                    await _ctx.SaveChangesAsync(); // guardamos para obtener PagNum

                    periodo.PagNum = pagoPeriodo.PagNum;
                }


                _ctx.PeriodosAbono.Add(periodo);
            }
        }

        // ==========================================================
        // ✅ Método GetPeriodosAbono corregido
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> GetPeriodosAbono(int plyID, int plzNum, DateTime aboFyhIni)
        {
            var fechaBase = DateTime.SpecifyKind(aboFyhIni, DateTimeKind.Utc);
            var hoy = DateTime.UtcNow.Date;

            var periodos = await _ctx.PeriodosAbono
                .Where(p => p.PlyID == plyID && p.PlzNum == plzNum && p.AboFyhIni.Date == fechaBase.Date)
                .OrderBy(p => p.PeriodoNumero)
                .ToListAsync();

            // En caso de no encontrar coincidencia exacta, buscar ±1 día
            if (!periodos.Any())
            {
                var fechaDesde = fechaBase.AddDays(-1);
                var fechaHasta = fechaBase.AddDays(1);

                periodos = await _ctx.PeriodosAbono
                    .Where(p => p.PlyID == plyID && p.PlzNum == plzNum &&
                                p.AboFyhIni >= fechaDesde && p.AboFyhIni <= fechaHasta)
                    .OrderBy(p => p.PeriodoNumero)
                    .ToListAsync();
            }

            var resultado = periodos.Select(p =>
            {
                string estado = p.EstadoPeriodo; // 👈 usamos la propiedad calculada

                return new
                {
                    PeriodoNumero = p.PeriodoNumero,
                    FechaInicio = p.PeriodoFechaInicio.ToString("dd/MM/yyyy"),
                    FechaFin = p.PeriodoFechaFin.ToString("dd/MM/yyyy"),
                    Monto = p.PeriodoMonto,
                    Estado = estado,
                    Pagado = p.PeriodoPagado,
                    FechaPago = p.PeriodoFechaPago.HasValue
                        ? p.PeriodoFechaPago.Value.ToString("dd/MM/yyyy")
                        : null
                };
            });

            return Json(resultado);
        }


        [HttpPost]
        public async Task<IActionResult> UpdateVehiculosAbono([FromBody] UpdateVehiculosAbonoVM model)
        {
            if (model == null || model.Vehiculos == null || model.Vehiculos.Count == 0)
                return Json(new { success = false, message = "Debe incluir al menos un vehículo." });

            try
            {
                var fechaBase = DateTime.SpecifyKind(model.AboFyhIni, DateTimeKind.Utc);

                // Buscar el abono sin tracking para evitar problemas de contexto
                var abono = await _ctx.Abonos
                    .Include(a => a.Vehiculos)
                    .FirstOrDefaultAsync(a =>
                        a.PlyID == model.PlyID &&
                        a.PlzNum == model.PlzNum &&
                        a.AboFyhIni >= fechaBase.AddSeconds(-1) &&
                        a.AboFyhIni <= fechaBase.AddSeconds(1));



                if (abono == null)
                    return Json(new { success = false, message = "No se encontró el abono especificado." });

                // 1️⃣ Eliminar asociaciones previas directamente en la tabla intermedia
                var existentes = await _ctx.VehiculosAbonados
                    .Where(v => v.PlyID == model.PlyID &&
                                v.PlzNum == model.PlzNum &&
                                v.AboFyhIni >= fechaBase.AddSeconds(-1) &&
                                v.AboFyhIni <= fechaBase.AddSeconds(1))
                    .ToListAsync();

                _ctx.VehiculosAbonados.RemoveRange(existentes);
                await _ctx.SaveChangesAsync();

                // 2️⃣ Asegurar existencia de cada vehículo y crear nuevas asociaciones
                var nuevasAsociaciones = new List<VehiculoAbonado>();

                foreach (var v in model.Vehiculos)
                {
                    // Buscar vehículo existente
                    var vehiculo = await _ctx.Vehiculos.FindAsync(v.VehPtnt);

                    if (vehiculo == null)
                    {
                        // Si no existe, crear nuevo con la clasificación recibida o 1 como fallback
                        vehiculo = new Vehiculo
                        {
                            VehPtnt = v.VehPtnt,
                            ClasVehID = model.ClasVehID > 0 ? model.ClasVehID : 1
                        };
                        _ctx.Vehiculos.Add(vehiculo);
                        await _ctx.SaveChangesAsync();
                        Console.WriteLine($"Vehículo nuevo creado: {vehiculo.VehPtnt} (ClasVehID={vehiculo.ClasVehID})");
                    }
                    else
                    {
                        // ✅ Mantener la clasificación original, no sobrescribirla
                        Console.WriteLine($"Vehículo {vehiculo.VehPtnt} ya existe (ClasVehID={vehiculo.ClasVehID})");
                    }

                    // Crear la asociación del vehículo con el abono
                    nuevasAsociaciones.Add(new VehiculoAbonado
                    {
                        PlyID = model.PlyID,
                        PlzNum = model.PlzNum,
                        AboFyhIni = fechaBase,
                        VehPtnt = v.VehPtnt
                    });
                }


                _ctx.VehiculosAbonados.AddRange(nuevasAsociaciones);
                await _ctx.SaveChangesAsync();
                Console.WriteLine($"Vehículos guardados: {nuevasAsociaciones.Count}");


                return Json(new { success = true, message = "Vehículos actualizados correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error actualizando vehículos: {ex.Message}" });
            }
        }

        // 🔹 Nuevos endpoints para gestión de pagos
        [HttpGet]
        public async Task<IActionResult> GetAbonoParaGestionarPagos(int plyID, int plzNum, DateTime aboFyhIni)
        {
            try
            {
                // Convertir la fecha a UTC para evitar problemas con PostgreSQL
                var fechaUTC = DateTime.SpecifyKind(aboFyhIni, DateTimeKind.Utc);
                
                var abono = await _ctx.Abonos
                    .Include(a => a.Abonado)
                    .Include(a => a.Periodos.OrderBy(p => p.PeriodoNumero))
                        .ThenInclude(p => p.Pago)
                    .Include(a => a.Vehiculos)
                        .ThenInclude(v => v.Vehiculo)
                            .ThenInclude(v => v.Clasificacion)
                    .FirstOrDefaultAsync(a =>
                        a.PlyID == plyID &&
                        a.PlzNum == plzNum &&
                        a.AboFyhIni >= fechaUTC.AddSeconds(-1) &&
                        a.AboFyhIni <= fechaUTC.AddSeconds(1));


                if (abono == null)
                {
                    return Json(new { success = false, message = "Abono no encontrado." });
                }

                var abonoData = new
                {
                    success = true,
                    abono = new
                    {
                        plyID = abono.PlyID,
                        plzNum = abono.PlzNum,
                        aboFyhIni = abono.AboFyhIni,
                        aboFyhFin = abono.AboFyhFin,
                        estadoPago = abono.EstadoPago.ToString(),
                        abonado = new
                        {
                            nombre = abono.Abonado.AboNom,
                            dni = abono.Abonado.AboDNI
                        },
                        periodos = abono.Periodos.Select(p => new
                        {
                            perNum = p.PeriodoNumero,
                            perFyhIni = p.PeriodoFechaInicio,
                            perFyhFin = p.PeriodoFechaFin,
                            // 🔹 Convertimos el monto a decimal fijo con dos decimales
                            perMonto = Math.Round(p.PeriodoMonto, 2),
                            // 🔹 EstadoPago correcto (Pagado / Pendiente)
                            estadoPago = p.PeriodoPagado ? "Pagado" : "Pendiente",
                            // 🔹 Fecha de pago y número asociados (si existen)
                            fechaPago = p.Pago?.PagFyh,
                            pagoNum = p.Pago?.PagNum
                        }).ToList(),
                        vehiculos = abono.Vehiculos.Select(v => new
                        {
                            patente = v.VehPtnt,
                            clasificacion = v.Vehiculo?.Clasificacion?.ClasVehTipo ?? "Sin clasificación"
                        }).ToList()

                    }
                };
                Console.WriteLine($"GetAbonoParaGestionarPagos → Enviando {abono.Periodos.Count} períodos, total: {abono.Periodos.Sum(p => p.PeriodoMonto)}");

                return Json(abonoData);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error obteniendo datos del abono: {ex.Message}" });
            }
        }

        // ======================================================
        // ✅ Nuevo método: obtener datos básicos del abono para la extensión
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> GetAbonoParaExtension(int plyID, int plzNum, DateTime aboFyhIni)
        {
            try
            {
                var fechaUTC = DateTime.SpecifyKind(aboFyhIni, DateTimeKind.Utc);

                var abono = await _ctx.Abonos
                    .Include(a => a.Abonado)
                    .Include(a => a.Plaza)
                    .Include(a => a.Periodos.OrderBy(p => p.PeriodoNumero))
                    .Include(a => a.Vehiculos).ThenInclude(v => v.Vehiculo).ThenInclude(v => v.Clasificacion)
                    .FirstOrDefaultAsync(a =>
                        a.PlyID == plyID &&
                        a.PlzNum == plzNum &&
                        a.AboFyhIni == fechaUTC);

                if (abono == null)
                    return Json(new { success = false, message = "Abono no encontrado." });

                    // 🔹 La fecha de fin real del abono es la del campo AboFyhFin
                    //    no la del último período (ya que puede tener un desfase de -1 día)
                    var fechaFinAbono = abono.AboFyhFin;

                    // 🔹 Mantener también la compatibilidad con los períodos
                    var fechaFinUltimoPeriodo = abono.Periodos
                        .OrderByDescending(p => p.PeriodoNumero)
                        .FirstOrDefault()?.PeriodoFechaFin;

                    var fechaFinActual = fechaFinAbono ?? fechaFinUltimoPeriodo;


                // Determinar tipo de abono basado en la duración del período
                string tipoAbono = "por día"; // Por defecto
                decimal tarifaReal = 0;
                
                if (abono.Periodos.Any())
                {
                    var primerPeriodo = abono.Periodos.OrderBy(p => p.PeriodoNumero).First();
                    tarifaReal = primerPeriodo.PeriodoMonto;
                    
                    // Determinar tipo por duración del período con margen de tolerancia
                    var duracion = (primerPeriodo.PeriodoFechaFin - primerPeriodo.PeriodoFechaInicio).TotalDays + 1;
                    
                    if (duracion >= 1 && duracion < 2)
                        tipoAbono = "por día";
                    else if (duracion >= 6 && duracion <= 8)
                        tipoAbono = "por semana";
                    else if (duracion >= 28 && duracion <= 31)
                        tipoAbono = "por mes";
                    else
                        tipoAbono = "por día"; // fallback
                }

                // Convertir estado del enum al texto
                string estadoTexto = abono.EstadoPago switch
                {
                    EstadoPago.Activo => "Al Día",
                    EstadoPago.Pendiente => "Pendiente",
                    EstadoPago.Finalizado => "Finalizado",
                    EstadoPago.Cancelado => "Cancelado",
                    _ => "Pendiente"
                };

                return Json(new
                {
                    success = true,
                    plyID = abono.PlyID,
                    aboFyhIni = abono.AboFyhIni,
                    fechaFinAbono = fechaFinAbono?.ToString("dd/MM/yyyy"), // 👈 agregado
                    abonado = new
                    {
                        nombre = abono.Abonado?.AboNom ?? "Sin nombre",
                        dni = abono.Abonado?.AboDNI ?? "N/A",
                        plaza = abono.PlzNum,
                        estado = estadoTexto,
                        fechaInicio = abono.AboFyhIni.ToString("dd/MM/yyyy"),
                        fechaFinActual = fechaFinActual?.ToString("dd/MM/yyyy")
                    },
                    vehiculos = abono.Vehiculos.Select(v => new
                    {
                        patente = v.VehPtnt,
                        clasificacion = v.Vehiculo?.Clasificacion?.ClasVehTipo ?? "Sin clasificación"
                    }).ToList(),
                    tipoAbono = tipoAbono
                });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al cargar datos del abono: {ex.Message}" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> RegistrarPagosPeriodos([FromBody] RegistrarPagosRequest request)
        {
            try
            {
                if (request == null)
                    return Json(new { success = false, message = "Solicitud vacía o inválida." });

                if (request.PeriodosAPagar == null || !request.PeriodosAPagar.Any())
                    return Json(new { success = false, message = "No se recibieron los períodos a pagar." });

                if (request.MetodoPago <= 0)
                    return Json(new { success = false, message = "Debe seleccionar un método de pago válido." });

                // 🔹 Normalizar fecha del abono
                var fechaUTC = DateTime.SpecifyKind(request.AboFyhIni, DateTimeKind.Utc);
                var fechaDesde = fechaUTC.AddSeconds(-2);
                var fechaHasta = fechaUTC.AddSeconds(2);

                // 🔹 Buscar abono
                var abono = await _ctx.Abonos
                    .Include(a => a.Periodos)
                    .FirstOrDefaultAsync(a =>
                        a.PlyID == request.PlyID &&
                        a.PlzNum == request.PlzNum &&
                        a.AboFyhIni >= fechaDesde && a.AboFyhIni <= fechaHasta);

                if (abono == null)
                    return Json(new { success = false, message = "No se encontró el abono con los datos proporcionados." });

                // 🔹 Calcular número de pago único (evita duplicados)
                var nextPagNum = (await _ctx.Pagos
                    .Where(p => p.PlyID == request.PlyID)
                    .Select(p => (int?)p.PagNum)
                    .MaxAsync() ?? 0) + 1;

                // 🔹 Crear nuevo registro de pago
                // Asegurar que la fecha de pago esté en UTC para PostgreSQL
                var fechaPagoUtc = request.FechaPago.Kind == DateTimeKind.Utc 
                    ? request.FechaPago 
                    : DateTime.SpecifyKind(request.FechaPago, DateTimeKind.Utc);
                
                var pago = new Pago
                {
                    PlyID = request.PlyID,
                    PagNum = nextPagNum,
                    PagFyh = fechaPagoUtc,
                    PagMonto = request.TotalPagar,
                    MepID = request.MetodoPago
                };

                _ctx.Pagos.Add(pago);
                await _ctx.SaveChangesAsync();

                // 🔹 Actualizar los períodos pagados
                foreach (var perNum in request.PeriodosAPagar)
                {
                    var periodo = abono.Periodos.FirstOrDefault(p => p.PeriodoNumero == perNum);
                    if (periodo != null)
                    {
                        periodo.PeriodoPagado = true;
                        periodo.PeriodoFechaPago = DateTime.SpecifyKind(pago.PagFyh, DateTimeKind.Utc);
                        periodo.PeriodoFechaInicio = DateTime.SpecifyKind(periodo.PeriodoFechaInicio, DateTimeKind.Utc);
                        periodo.PeriodoFechaFin = DateTime.SpecifyKind(periodo.PeriodoFechaFin, DateTimeKind.Utc);
                        periodo.PagNum = pago.PagNum;

                        _ctx.PeriodosAbono.Update(periodo);
                    }
                }

                await _ctx.SaveChangesAsync();

              // 🔹 Recalcular el estado del abono de forma correcta
                var hoy = DateTime.Now;
                var totalPeriodos = abono.Periodos.Count;
                var pagados = abono.Periodos.Count(p => p.PeriodoPagado);
                var pendientes = totalPeriodos - pagados;

                // Calcular el estado usando la misma lógica que CalcularEstadoTexto
                var estadoTexto = CalcularEstadoTexto(abono, hoy);
                
                // 🔹 DEBUG: Log para verificar el cálculo
                Console.WriteLine($"🔹 DEBUG - Fecha hoy: {hoy:dd/MM/yyyy}");
                Console.WriteLine($"🔹 DEBUG - Estado calculado: {estadoTexto}");
                Console.WriteLine($"🔹 DEBUG - Períodos: {abono.Periodos.Count} total, {pagados} pagados, {pendientes} pendientes");
                
                // Convertir el texto del estado al enum correspondiente
                abono.EstadoPago = estadoTexto switch
                {
                    "Al Día" => EstadoPago.Activo,
                    "Pendiente" => EstadoPago.Pendiente,
                    "Finalizado" => EstadoPago.Finalizado,
                    "Cancelado" => EstadoPago.Cancelado,
                    _ => EstadoPago.Pendiente
                };
                
                Console.WriteLine($"🔹 DEBUG - EstadoPago asignado: {abono.EstadoPago}");

                _ctx.Abonos.Update(abono);
                await _ctx.SaveChangesAsync();


                // 🔹 Devolver información actualizada al frontend (versión corregida)
                return Json(new
                {
                    success = true,
                    message = "Pagos registrados correctamente.",
                    resumen = new
                    {
                        total = totalPeriodos,
                        pagados,
                        pendientes
                    },
                    nuevoEstado = new
                    {
                        texto = CalcularEstadoTexto(abono, hoy),
                        color = CalcularEstadoColor(abono, hoy)
                    }
                });


            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = $"Error registrando pagos: {msg}" });
            }
        }

        // ======================================================
        // ✅ Extensión de abono existente en la misma plaza
        // ======================================================
        [HttpPost]
        public async Task<IActionResult> ExtenderAbonoPorPeriodos([FromBody] ExtenderAbonoRequest request)
        {
            try
            {
                var fechaUTC = DateTime.SpecifyKind(request.aboFyhIni, DateTimeKind.Utc);

                // 🔹 Buscar abono original
                var abono = await _ctx.Abonos
                    .Include(a => a.Periodos)
                    .FirstOrDefaultAsync(a =>
                        a.PlyID == request.plyID &&
                        a.PlzNum == request.plzNum &&
                        a.AboFyhIni == fechaUTC);

                if (abono == null)
                    return Json(new { success = false, message = "Abono original no encontrado." });

                // 🔹 Buscar el último período del abono actual
                var ultimoPeriodo = abono.Periodos
                    .OrderByDescending(p => p.PeriodoNumero)
                    .FirstOrDefault();

                if (ultimoPeriodo == null)
                    return Json(new { success = false, message = "No se encontraron períodos asociados al abono." });
                // ✅ Crear fecha de inicio de extensión como UTC real (sin heredar Unspecified)
                var finUltimoPeriodo = ultimoPeriodo.PeriodoFechaFin;
                DateTime fechaInicioExtension = new DateTime(
                    finUltimoPeriodo.Year,
                    finUltimoPeriodo.Month,
                    finUltimoPeriodo.Day,
                    finUltimoPeriodo.Hour,
                    finUltimoPeriodo.Minute,
                    finUltimoPeriodo.Second,
                    DateTimeKind.Utc
                );
                
                // 🔹 Validar que la cantidad de períodos sea positiva
                if (request.cantidadPeriodos <= 0)
                {
                    return Json(new { success = false, message = "La cantidad de períodos debe ser mayor a cero." });
                }

                // 🔹 Obtener tarifa real del abono original
                var primerPeriodo = abono.Periodos.OrderBy(p => p.PeriodoNumero).First();
                var tarifaPorPeriodo = primerPeriodo.PeriodoMonto;
                
                // 🔹 Determinar el tipo real del abono original por duración
                string tipoAbonoOriginal = "por día";
                var duracion = (primerPeriodo.PeriodoFechaFin - primerPeriodo.PeriodoFechaInicio).TotalDays + 1;
                
                if (duracion >= 1 && duracion < 2)
                    tipoAbonoOriginal = "por día";
                else if (duracion >= 6 && duracion <= 8)
                    tipoAbonoOriginal = "por semana";
                else if (duracion >= 28 && duracion <= 31)
                    tipoAbonoOriginal = "por mes";
                
                // 🔹 Validar que el tipo de extensión coincida con el tipo original
                if (!string.Equals(request.tipoExtension.Trim(), tipoAbonoOriginal, StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new
                    {
                        success = false,
                        message = "El tipo de abono no coincide. Debe crear un nuevo abono si desea cambiar la modalidad.",
                        redirect = true
                    });
                }

                // 🔹 Calcular fecha de fin según el tipo de abono y la cantidad de períodos
                DateTime fechaFinExtension;
                // 🔹 A todas las extensiones se les suma +1 día adicional
                switch (tipoAbonoOriginal.ToLower().Trim())
                {
                    case "por día":
                        fechaFinExtension = fechaInicioExtension.AddDays(request.cantidadPeriodos + 1);
                        break;
                    case "por semana":
                        fechaFinExtension = fechaInicioExtension.AddDays((request.cantidadPeriodos * 7) + 1);
                        break;
                    case "por mes":
                        fechaFinExtension = fechaInicioExtension.AddMonths(request.cantidadPeriodos).AddDays(1);
                        break;
                    default:
                        fechaFinExtension = fechaInicioExtension.AddDays(request.cantidadPeriodos + 1);
                        break;
                }

                // 🔹 Verificar disponibilidad de la plaza (excluyendo el abono actual)
                var ocupada = await _ctx.Abonos.AnyAsync(a =>
                    a.PlyID == request.plyID &&
                    a.PlzNum == request.plzNum &&
                    a.AboFyhIni != fechaUTC && // Excluir el abono actual
                    a.EstadoPago != EstadoPago.Cancelado &&
                    ((a.AboFyhIni <= fechaFinExtension && a.AboFyhFin >= fechaInicioExtension)));

                if (ocupada)
                {
                    return Json(new
                    {
                        success = false,
                        message = "La plaza está ocupada en las fechas seleccionadas.",
                        redirect = true
                    });
                }

               
            // 🔹 Crear los nuevos períodos con la misma estructura que el Create
                DateTime fechaInicioPeriodo = fechaInicioExtension;

                for (int i = 1; i <= request.cantidadPeriodos; i++)
                {
                    DateTime fechaFinPeriodo;

                    switch (tipoAbonoOriginal.ToLower().Trim())
                    {
                        case "por día":
                            fechaFinPeriodo = fechaInicioPeriodo.AddDays(1); // +1 día
                            break;
                        case "por semana":
                            fechaFinPeriodo = fechaInicioPeriodo.AddDays(8); // 7 + 1 día extra
                            break;
                        case "por mes":
                            fechaFinPeriodo = fechaInicioPeriodo.AddMonths(1).AddDays(1); // +1 día extra
                            break;
                        default:
                            fechaFinPeriodo = fechaInicioPeriodo.AddDays(1);
                            break;
                    }

                        fechaInicioPeriodo = DateTime.SpecifyKind(fechaInicioPeriodo, DateTimeKind.Utc);
                        fechaFinPeriodo = DateTime.SpecifyKind(fechaFinPeriodo, DateTimeKind.Utc);

                    // 🔹 Obtener el siguiente número de período disponible
                    var maxPeriodoNumero = abono.Periodos.Any() 
                        ? abono.Periodos.Max(p => p.PeriodoNumero) 
                        : 0;
                    
                    // 🔹 Forzar todas las fechas a UTC antes de guardar
                    var periodo = new PeriodoAbono
                    {
                        PlyID = abono.PlyID,
                        PlzNum = abono.PlzNum,
                        AboFyhIni = DateTime.SpecifyKind(abono.AboFyhIni, DateTimeKind.Utc),
                        PeriodoNumero = maxPeriodoNumero + i,
                        PeriodoFechaInicio = DateTime.SpecifyKind(fechaInicioPeriodo, DateTimeKind.Utc),
                        PeriodoFechaFin = DateTime.SpecifyKind(fechaFinPeriodo, DateTimeKind.Utc),
                        PeriodoMonto = tarifaPorPeriodo,
                        PeriodoPagado = false
                    };

                    _ctx.PeriodosAbono.Add(periodo);

                    // Siguiente período comienza al día siguiente del fin
                    fechaInicioPeriodo = fechaFinPeriodo.AddDays(1);
                }

                // 🔹 Actualizar fecha fin del abono
                abono.AboFyhFin = fechaFinExtension;
                _ctx.Abonos.Update(abono);

                // 🔹 Normalizar TODAS las fechas a UTC (evita mezcla de Kinds)
                foreach (var periodo in abono.Periodos)
                {
                    periodo.PeriodoFechaInicio = DateTime.SpecifyKind(periodo.PeriodoFechaInicio, DateTimeKind.Utc);
                    periodo.PeriodoFechaFin = DateTime.SpecifyKind(periodo.PeriodoFechaFin, DateTimeKind.Utc);

                    if (periodo.PeriodoFechaPago.HasValue)
                        periodo.PeriodoFechaPago = DateTime.SpecifyKind(periodo.PeriodoFechaPago.Value, DateTimeKind.Utc);
                }

                // 🔹 También normalizar el abono principal
                abono.AboFyhIni = DateTime.SpecifyKind(abono.AboFyhIni, DateTimeKind.Utc);
                if (abono.AboFyhFin.HasValue)
                    abono.AboFyhFin = DateTime.SpecifyKind(abono.AboFyhFin.Value, DateTimeKind.Utc);

                // 🔹 Y los nuevos períodos creados
                var periodosNuevos = _ctx.ChangeTracker.Entries<PeriodoAbono>()
                    .Where(e => e.State == EntityState.Added)
                    .Select(e => e.Entity)
                    .ToList();

                foreach (var periodo in periodosNuevos)
                {
                    periodo.PeriodoFechaInicio = DateTime.SpecifyKind(periodo.PeriodoFechaInicio, DateTimeKind.Utc);
                    periodo.PeriodoFechaFin = DateTime.SpecifyKind(periodo.PeriodoFechaFin, DateTimeKind.Utc);

                    if (periodo.PeriodoFechaPago.HasValue)
                        periodo.PeriodoFechaPago = DateTime.SpecifyKind(periodo.PeriodoFechaPago.Value, DateTimeKind.Utc);
                }


                await _ctx.SaveChangesAsync();

                return Json(new { success = true, message = "Extensión realizada correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al extender abono: {ex.Message}" });
            }
        }

        private decimal ObtenerTarifaPorPeriodo(int plyID, string tipoExtension)
        {
            // Por ahora, usar tarifas por defecto
            return tipoExtension switch
            {
                "por día" => 100m,
                "por semana" => 600m,
                "por mes" => 2400m,
                _ => 100m
            };
        }


        [HttpPost]
        public async Task<IActionResult> ExtenderAbono([FromBody] ExtenderAbonoRequest request)
        {
            try
            {
                // Convertir la fecha a UTC para evitar problemas con PostgreSQL
                var fechaUTC = DateTime.SpecifyKind(request.aboFyhIni, DateTimeKind.Utc);
                
                var abonoOriginal = await _ctx.Abonos
                    .Include(a => a.Abonado)
                    .Include(a => a.Vehiculos)
                        .ThenInclude(v => v.Vehiculo)
                    .FirstOrDefaultAsync(a => a.PlyID == request.plyID && 
                                            a.PlzNum == request.plzNum && 
                                            a.AboFyhIni == fechaUTC);

                if (abonoOriginal == null)
                {
                    return Json(new { success = false, message = "Abono original no encontrado." });
                }

                // Validar que la plaza no esté ocupada en el nuevo período
                var fechaInicioNueva = abonoOriginal.AboFyhFin?.AddDays(1) ?? DateTime.Now.AddDays(1);
                var fechaFinNueva = CalcularFechaFin(fechaInicioNueva, request.tipoExtension, request.cantidadPeriodos);

                var plazaOcupada = await _ctx.Abonos
                    .AnyAsync(a => a.PlyID == request.plyID && a.PlzNum == request.plzNum &&
                                 a.EstadoPago != EstadoPago.Cancelado &&
                                 ((a.AboFyhIni <= fechaInicioNueva && a.AboFyhFin >= fechaInicioNueva) ||
                                  (a.AboFyhIni <= fechaFinNueva && a.AboFyhFin >= fechaFinNueva)));

                if (plazaOcupada)
                {
                    return Json(new { success = false, message = "La plaza está ocupada en el período solicitado." });
                }

                // Obtener el servicio correspondiente
                var servicio = await _ctx.Servicios
                    .FirstOrDefaultAsync(s => s.SerNom == $"Abono por 1 {request.tipoExtension.ToLower()}");

                if (servicio == null)
                {
                    return Json(new { success = false, message = "Servicio no encontrado." });
                }

                // Crear nuevo abono
                var nuevoAbono = new Abono
                {
                    PlyID = abonoOriginal.PlyID,
                    PlzNum = abonoOriginal.PlzNum,
                    AboFyhIni = fechaInicioNueva,
                    AboFyhFin = fechaFinNueva,
                    AboDNI = abonoOriginal.AboDNI,
                    EstadoPago = EstadoPago.Pendiente,
                    PagNum = 0 // Se asignará cuando se haga el primer pago
                };

                _ctx.Abonos.Add(nuevoAbono);

                // Obtener tarifa por período
                var tarifaPorPeriodo = ObtenerTarifaPorPeriodo(request.plyID, request.tipoExtension);

                // Crear períodos para el nuevo abono
                var fechaActual = fechaInicioNueva;
                for (int i = 1; i <= request.cantidadPeriodos; i++)
                {
                    var fechaFinPeriodo = CalcularFechaFin(fechaActual, request.tipoExtension, 1);
                    
                    var periodo = new PeriodoAbono
                    {
                        PlyID = nuevoAbono.PlyID,
                        PlzNum = nuevoAbono.PlzNum,
                        AboFyhIni = nuevoAbono.AboFyhIni,
                        PeriodoNumero = i,
                        PeriodoFechaInicio = fechaActual,
                        PeriodoFechaFin = fechaFinPeriodo,
                        PeriodoMonto = tarifaPorPeriodo,
                        PeriodoPagado = false
                    };

                    _ctx.PeriodosAbono.Add(periodo);
                    fechaActual = fechaFinPeriodo.AddDays(1);
                }

                // Copiar vehículos del abono original
                foreach (var vehiculoAbonado in abonoOriginal.Vehiculos)
                {
                    var nuevoVehiculoAbonado = new VehiculoAbonado
                    {
                        PlyID = nuevoAbono.PlyID,
                        PlzNum = nuevoAbono.PlzNum,
                        AboFyhIni = nuevoAbono.AboFyhIni,
                        VehPtnt = vehiculoAbonado.VehPtnt
                    };
                    _ctx.VehiculosAbonados.Add(nuevoVehiculoAbonado);
                }

               await _ctx.SaveChangesAsync();

                // ✅ Actualizar la fecha fin del abono anterior en el Index
                var abonoViejo = await _ctx.Abonos
                    .FirstOrDefaultAsync(a => a.PlyID == request.plyID && a.PlzNum == request.plzNum && a.AboFyhIni == fechaUTC);

                if (abonoViejo != null)
                {
                    abonoViejo.AboFyhFin = fechaFinNueva;
                    _ctx.Abonos.Update(abonoViejo);
                    await _ctx.SaveChangesAsync();
                }

                return Json(new
                {
                    success = true,
                    message = "Abono extendido correctamente.",
                    nuevoAbonoId = $"{nuevoAbono.PlyID}-{nuevoAbono.PlzNum}-{nuevoAbono.AboFyhIni:yyyy-MM-ddTHH:mm:ss}"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error extendiendo abono: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTarifaServicio(int plyID, string tipoExtension)
        {
            try
            {
                var servicio = await _ctx.Servicios
                    .FirstOrDefaultAsync(s => s.SerNom == $"Abono por 1 {tipoExtension.ToLower()}");

                if (servicio == null)
                {
                    return Json(new { success = false, message = "Servicio no encontrado." });
                }

                var tarifa = await _ctx.TarifasServicio
                    .Where(ts => ts.SerID == servicio.SerID)
                    .OrderByDescending(ts => ts.TasFecIni)
                    .FirstOrDefaultAsync();

                if (tarifa == null)
                {
                    return Json(new { success = false, message = "Tarifa no encontrada." });
                }

                return Json(new { success = true, tarifa = tarifa.TasMonto });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error obteniendo tarifa: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTarifaRealAbono(int plyID, int plzNum, DateTime aboFyhIni)
        {
            try
            {
                // Convertir la fecha a UTC para evitar problemas con PostgreSQL
                var fechaUTC = DateTime.SpecifyKind(aboFyhIni, DateTimeKind.Utc);
                
                // Buscar el abono y sus períodos
                var abono = await _ctx.Abonos
                    .Include(a => a.Periodos)
                    .FirstOrDefaultAsync(a => a.PlyID == plyID && a.PlzNum == plzNum && a.AboFyhIni == fechaUTC);

                if (abono == null || !abono.Periodos.Any())
                {
                    return Json(new { success = false, message = "Abono no encontrado o sin períodos." });
                }

                // Obtener el primer período para determinar el tipo y tarifa
                var primerPeriodo = abono.Periodos.OrderBy(p => p.PeriodoNumero).First();
                var tarifa = primerPeriodo.PeriodoMonto;
                
                // Determinar tipo de abono por duración del período con margen de tolerancia
                string tipoAbono = "por día";
                var duracion = (primerPeriodo.PeriodoFechaFin - primerPeriodo.PeriodoFechaInicio).TotalDays + 1;
                
                if (duracion >= 1 && duracion < 2)
                    tipoAbono = "por día";
                else if (duracion >= 6 && duracion <= 8)
                    tipoAbono = "por semana";
                else if (duracion >= 28 && duracion <= 31)
                    tipoAbono = "por mes";
                else
                    tipoAbono = "por día"; // fallback

                return Json(new { 
                    success = true, 
                    tarifa = tarifa,
                    tipoAbono = tipoAbono
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        private DateTime CalcularFechaFin(DateTime fechaInicio, string tipoExtension, int cantidadPeriodos)
        {
            return tipoExtension switch
            {
                "Diario" => fechaInicio.AddDays(cantidadPeriodos),
                "Semanal" => fechaInicio.AddDays(cantidadPeriodos * 7),
                "Mensual" => fechaInicio.AddMonths(cantidadPeriodos),
                _ => fechaInicio
            };
        }
        // =========================================================
        // 🔹 FUNCIONES AUXILIARES PARA ESTADO DE PAGO DEL ABONO
        // =========================================================
        private string CalcularEstadoTexto(Abono abono, DateTime hoy)
        {
            var hoyDate = hoy.Date;

            // 🔹 VERIFICAR PRIMERO SI EL ABONO YA ESTÁ CANCELADO
            if (abono.EstadoPago == EstadoPago.Cancelado)
            {
                Console.WriteLine("🔹 RESULTADO: Cancelado (ya estaba cancelado en BD)");
                return "Cancelado";
            }

            // 🔹 DEBUG: Log detallado
            Console.WriteLine($"🔹 CalcularEstadoTexto - Fecha hoy: {hoyDate:dd/MM/yyyy}");
            Console.WriteLine($"🔹 CalcularEstadoTexto - Abono fechas: {abono.AboFyhIni:dd/MM/yyyy} - {abono.AboFyhFin?.ToString("dd/MM/yyyy") ?? "Sin fin"}");
            Console.WriteLine($"🔹 CalcularEstadoTexto - Períodos totales: {abono.Periodos.Count}");
            foreach (var p in abono.Periodos.OrderBy(x => x.PeriodoNumero))
            {
                Console.WriteLine($"   Período {p.PeriodoNumero}: {p.PeriodoFechaInicio:dd/MM/yyyy} - {p.PeriodoFechaFin:dd/MM/yyyy}, Pagado: {p.PeriodoPagado}");
            }

            // 🔹 PASO 1: Si el abono terminó su rango de fechas
            if (abono.AboFyhFin.HasValue && hoyDate > abono.AboFyhFin.Value.Date)
            {
                // Si terminó el rango pero todos los períodos están pagados → Finalizado
                if (abono.Periodos.All(p => p.PeriodoPagado))
                {
                    Console.WriteLine("🔹 RESULTADO: Finalizado (abono terminó y todos los períodos están pagados)");
                    return "Finalizado";
                }
                // Si terminó el rango pero quedaron períodos pendientes → Pendiente
                else
                {
                    Console.WriteLine("🔹 RESULTADO: Pendiente (abono terminó pero quedaron períodos impagos)");
                    return "Pendiente";
                }
            }

            // 🔹 PASO 2: Si el abono está dentro de su rango de fechas o no tiene fecha fin
            // Buscar el período actual donde está parado hoy
            var periodoActual = abono.Periodos
                .Where(p => hoyDate >= p.PeriodoFechaInicio.Date && hoyDate <= p.PeriodoFechaFin.Date)
                .FirstOrDefault();

            Console.WriteLine($"🔹 PASO 2 - Período actual: {(periodoActual != null ? $"Período {periodoActual.PeriodoNumero} (Pagado: {periodoActual.PeriodoPagado})" : "Ninguno")}");

            // 🔹 PASO 3: Si estoy dentro de un período específico
            if (periodoActual != null)
            {
                // Si el período actual está pagado → Al Día
                if (periodoActual.PeriodoPagado)
                {
                    Console.WriteLine("🔹 RESULTADO: Al Día (período actual pagado)");
                    return "Al Día";
                }
                // Si el período actual no está pagado → Pendiente
                else
                {
                    Console.WriteLine("🔹 RESULTADO: Pendiente (período actual no pagado)");
                    return "Pendiente";
                }
            }

            // 🔹 PASO 4: Si no estoy dentro de ningún período pero el abono está vigente
            // Verificar si hay períodos vencidos sin pagar
            var periodosVencidosSinPagar = abono.Periodos
                .Any(p => !p.PeriodoPagado && p.PeriodoFechaFin.Date < hoyDate);

            Console.WriteLine($"🔹 PASO 4 - Períodos vencidos sin pagar: {periodosVencidosSinPagar}");

            if (periodosVencidosSinPagar)
            {
                Console.WriteLine("🔹 RESULTADO: Pendiente (hay períodos vencidos sin pagar)");
                return "Pendiente";
            }

            // 🔹 PASO 5: Si no hay períodos vencidos, verificar si estoy en el rango de períodos pagados
            var ultimaFechaPagada = abono.Periodos
                .Where(p => p.PeriodoPagado)
                .Select(p => p.PeriodoFechaFin.Date)
                .DefaultIfEmpty(DateTime.MinValue.Date)
                .Max();

            Console.WriteLine($"🔹 PASO 5 - Última fecha pagada: {ultimaFechaPagada:dd/MM/yyyy}");

            if (hoyDate <= ultimaFechaPagada)
            {
                Console.WriteLine("🔹 RESULTADO: Al Día (dentro del rango de períodos pagados)");
                return "Al Día";
            }

            // 🔹 PASO 6: Caso por defecto → Pendiente
            Console.WriteLine("🔹 RESULTADO: Pendiente (caso por defecto)");
            return "Pendiente";
        }

        [HttpGet]
        public async Task<IActionResult> GetPlazasPorPlaya(int plyID)

        {
            try
            {
                var plazas = await _ctx.Plazas
                    .Where(p => p.PlyID == plyID)
                    .Select(p => new { 
                        plzNum = p.PlzNum, 
                        piso = p.Piso,
                        habilitada = p.PlzHab 
                    })
                    .ToListAsync();

                return Json(plazas);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error obteniendo plazas: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTarifaExtension(int plyID, string tipoPeriodo, string clasificacion)
        {
            try
            {
                // Buscar tarifa en la base de datos
                var tarifa = await _ctx.TarifasServicio
                    .Include(t => t.ServicioProveido)
                        .ThenInclude(sp => sp.Servicio)
                    .Include(t => t.ClasificacionVehiculo)
                    .Where(t => t.PlyID == plyID && 
                               t.ServicioProveido.Servicio.SerNom.Contains(tipoPeriodo) &&
                               t.ClasificacionVehiculo.ClasVehTipo == clasificacion &&
                               (t.TasFecFin == null || t.TasFecFin > DateTime.Now))
                    .OrderByDescending(t => t.TasFecIni)
                    .Select(t => t.TasMonto)
                    .FirstOrDefaultAsync();

                if (tarifa == 0)
                {
                    // Tarifas por defecto si no se encuentra en la base de datos
                    tarifa = tipoPeriodo switch
                    {
                        "Diario" => 100m,
                        "Semanal" => 600m,
                        "Mensual" => 2400m,
                        _ => 100m
                    };
                }

                return Json(new { success = true, tarifa = tarifa });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error obteniendo tarifa: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CrearNuevoAbonoDesdeExtension([FromBody] CrearAbonoDesdeExtensionRequest request)
        {
            try
            {
                // Verificar disponibilidad de la plaza
                var fechaInicio = DateTime.Parse(request.fechaInicio);
                var fechaFin = CalcularFechaFinExtension(request.tipoPeriodo, request.cantidadPeriodos, fechaInicio);

                var plazaOcupada = await _ctx.Abonos
                    .AnyAsync(a => a.PlyID == request.plyID && 
                                  a.PlzNum == request.plzNum &&
                                  a.EstadoPago != EstadoPago.Cancelado &&
                                  ((a.AboFyhIni <= fechaInicio && a.AboFyhFin >= fechaInicio) ||
                                   (a.AboFyhIni <= fechaFin && a.AboFyhFin >= fechaFin) ||
                                   (a.AboFyhIni >= fechaInicio && a.AboFyhFin <= fechaFin)));

                if (plazaOcupada)
                {
                    return Json(new { success = false, message = "La plaza seleccionada no está disponible en las fechas indicadas" });
                }

                // Obtener tarifa
                var tarifaResponse = await GetTarifaExtension(request.plyID, request.tipoPeriodo, request.clasificacion);
                var tarifaResponseString = JsonSerializer.Serialize(tarifaResponse);
                var tarifaData = JsonSerializer.Deserialize<JsonElement>(tarifaResponseString);
                var tarifaPorPeriodo = tarifaData.GetProperty("tarifa").GetDecimal();

                // Crear nuevo abono
                var nuevoAbono = new Abono
                {
                    PlyID = request.plyID,
                    PlzNum = request.plzNum,
                    AboFyhIni = fechaInicio,
                    AboFyhFin = fechaFin,
                    AboMonto = tarifaPorPeriodo * request.cantidadPeriodos,
                    EstadoPago = EstadoPago.Pendiente,
                    AboDNI = request.abonado.dni
                };

                _ctx.Abonos.Add(nuevoAbono);
                await _ctx.SaveChangesAsync();

                // Crear períodos
                for (int i = 1; i <= request.cantidadPeriodos; i++)
                {
                    var fechaInicioPeriodo = CalcularFechaInicioPeriodo(request.tipoPeriodo, i, fechaInicio);
                    var fechaFinPeriodo = CalcularFechaFinPeriodo(request.tipoPeriodo, fechaInicioPeriodo);

                    var periodo = new PeriodoAbono
                    {
                        PlyID = request.plyID,
                        PlzNum = request.plzNum,
                        AboFyhIni = nuevoAbono.AboFyhIni,
                        PeriodoNumero = i,
                        PeriodoFechaInicio = fechaInicioPeriodo,
                        PeriodoFechaFin = fechaFinPeriodo,
                        PeriodoMonto = tarifaPorPeriodo,
                        PeriodoPagado = false
                    };

                    _ctx.PeriodosAbono.Add(periodo);
                }

                // Copiar vehículos
                foreach (var vehiculo in request.vehiculos)
                {
                    var vehiculoAbono = new VehiculoAbonado
                    {
                        PlyID = request.plyID,
                        PlzNum = request.plzNum,
                        AboFyhIni = nuevoAbono.AboFyhIni,
                        VehPtnt = vehiculo.patente
                    };
                    _ctx.VehiculosAbonados.Add(vehiculoAbono);
                }

                await _ctx.SaveChangesAsync();

                return Json(new { success = true, message = "Nuevo abono creado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error creando abono: {ex.Message}" });
            }
        }

        private DateTime CalcularFechaFinExtension(string tipoPeriodo, int cantidad, DateTime fechaInicio)
        {
            return tipoPeriodo switch
            {
                "Diario" => fechaInicio.AddDays(cantidad),
                "Semanal" => fechaInicio.AddDays(cantidad * 7),
                "Mensual" => fechaInicio.AddMonths(cantidad),
                _ => fechaInicio.AddDays(cantidad)
            };
        }

        private DateTime CalcularFechaInicioPeriodo(string tipoPeriodo, int numeroPeriodo, DateTime fechaInicio)
        {
            return tipoPeriodo switch
            {
                "Diario" => fechaInicio.AddDays(numeroPeriodo - 1),
                "Semanal" => fechaInicio.AddDays((numeroPeriodo - 1) * 7),
                "Mensual" => fechaInicio.AddMonths(numeroPeriodo - 1),
                _ => fechaInicio.AddDays(numeroPeriodo - 1)
            };
        }

        private DateTime CalcularFechaFinPeriodo(string tipoPeriodo, DateTime fechaInicioPeriodo)
        {
            return tipoPeriodo switch
            {
                "Diario" => fechaInicioPeriodo,
                "Semanal" => fechaInicioPeriodo.AddDays(7),
                "Mensual" => fechaInicioPeriodo.AddMonths(1),
                _ => fechaInicioPeriodo
            };
        }

        private string CalcularEstadoColor(Abono abono, DateTime hoy)
        {
            var texto = CalcularEstadoTexto(abono, hoy);

            return texto switch
            {
                "Al Día" => "text-success fw-bold",
                "Pendiente" => "text-warning fw-bold",
                "Finalizado" => "text-dark fw-bold",
                "Cancelado" => "text-danger fw-bold",
                _ => "text-muted fw-bold"
            };
        }

        [HttpPost]
        public async Task<IActionResult> CancelarAbono([FromBody] JsonElement data)
        {
            try
            {
                Console.WriteLine("🔍 CancelarAbono endpoint llamado");
                int plyID = data.GetProperty("plyID").GetInt32();
                int plzNum = data.GetProperty("plzNum").GetInt32();
                DateTime aboFyhIni = DateTime.Parse(data.GetProperty("aboFyhIni").GetString() ?? string.Empty);
                
                Console.WriteLine($"🔍 Parámetros recibidos: plyID={plyID}, plzNum={plzNum}, aboFyhIni={aboFyhIni}");

                // Forzar UTC (coherente con timestamp with time zone)
                aboFyhIni = DateTime.SpecifyKind(aboFyhIni, DateTimeKind.Utc);

                // 🔹 Buscar abono en BD (filtrando por ID y plaza primero)
                var posiblesAbonos = await _ctx.Abonos
                    .Include(a => a.Abonado)
                    .Where(a => a.PlyID == plyID && a.PlzNum == plzNum)
                    .ToListAsync();

                Console.WriteLine($"🔍 Encontrados {posiblesAbonos.Count} abonos para plyID={plyID}, plzNum={plzNum}");

                // 🔹 Luego filtrar en memoria por fecha con tolerancia de segundos
                var abono = posiblesAbonos
                    .FirstOrDefault(a => Math.Abs((a.AboFyhIni - aboFyhIni).TotalSeconds) < 1);

                if (abono == null)
                {
                    Console.WriteLine("🔍 No se encontró el abono especificado");
                    return Json(new { success = false, message = "No se encontró el abono especificado." });
                }

                Console.WriteLine($"🔍 Abono encontrado: EstadoPago={abono.EstadoPago}");

                if (abono.EstadoPago == EstadoPago.Cancelado)
                {
                    Console.WriteLine("🔍 El abono ya estaba cancelado");
                    return Json(new { success = false, message = "El abono ya estaba cancelado." });
                }

                // 🔹 Actualizar estado y fecha de fin
                Console.WriteLine("🔍 Actualizando abono a cancelado...");
                abono.EstadoPago = EstadoPago.Cancelado;
                abono.AboFyhFin = DateTime.UtcNow;

                _ctx.Abonos.Update(abono);
                await _ctx.SaveChangesAsync();

                Console.WriteLine("🔍 Abono cancelado exitosamente en la BD");

                return Json(new
                {
                    success = true,
                    message = $"El abono de {abono.Abonado.AboNom} ha sido cancelado correctamente."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Ocurrió un error al cancelar el abono: {ex.Message}"
                });
            }
        }


    }

    // 🔹 Clases para requests
    public class RegistrarPagosRequest
    {
        public int PlyID { get; set; }
        public int PlzNum { get; set; }
        public DateTime AboFyhIni { get; set; }
        public List<int> PeriodosAPagar { get; set; } = new List<int>();
        public int MetodoPago { get; set; }
        public DateTime FechaPago { get; set; }
        public decimal TotalPagar { get; set; }
    }

    public class ExtenderAbonoRequest
    {
        public int plyID { get; set; }
        public int plzNum { get; set; }
        public DateTime aboFyhIni { get; set; }
        public string tipoExtension { get; set; } = string.Empty;
        public int cantidadPeriodos { get; set; }
        public string fechaInicio { get; set; } = string.Empty;
    }

    public class CrearAbonoDesdeExtensionRequest
    {
        public int plyID { get; set; }
        public int plzNum { get; set; }
        public DateTime aboFyhIni { get; set; }
        public string tipoPeriodo { get; set; } = string.Empty;
        public int cantidadPeriodos { get; set; }
        public string fechaInicio { get; set; } = string.Empty;
        public string clasificacion { get; set; } = string.Empty;
        public AbonadoInfo abonado { get; set; } = new();
        public List<VehiculoInfo> vehiculos { get; set; } = new();
    }

    public class AbonadoInfo
    {
        public string nombre { get; set; } = string.Empty;
        public string dni { get; set; } = string.Empty;
    }

    public class VehiculoInfo
    {
        public string patente { get; set; } = string.Empty;
        public string clasificacion { get; set; } = string.Empty;
    }

    public class CancelarAbonoRequest
    {
        public int plyID { get; set; }
        public int plzNum { get; set; }
        public DateTime aboFyhIni { get; set; }
    }
}
