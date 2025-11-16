using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.Models.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace estacionamientos.Controllers
{
    [Authorize(Roles = "Playero")]
    public class ServicioExtraRealizadoController : BaseController
    {
        private readonly AppDbContext _ctx;
        public ServicioExtraRealizadoController(AppDbContext ctx) => _ctx = ctx;

        // 🔹 Cargar clasificaciones de vehículo
        private async Task LoadClasificacionesVehiculo(int? selected = null)
        {
            var clasificaciones = await _ctx.ClasificacionesVehiculo
                .OrderBy(c => c.ClasVehTipo)
                .AsNoTracking()
                .Select(c => new
                {
                    c.ClasVehID,
                    c.ClasVehTipo,
                    c.ClasVehDesc
                })
                .ToListAsync();

            ViewBag.ClasVehID = new SelectList(clasificaciones, "ClasVehID", "ClasVehTipo", selected);
            ViewBag.ClasificacionesDetalle = clasificaciones;
        }

        // 🔹 Cargar servicios extra habilitados para una playa
        private async Task LoadServiciosHabilitados(int plyID, int? serSel = null)
        {
            var servicios = await _ctx.ServiciosProveidos
                .Include(sp => sp.Servicio)
                .Where(sp => sp.PlyID == plyID &&
                             sp.SerProvHab &&
                             sp.Servicio.SerTipo == "ServicioExtra")
                .AsNoTracking()
                .OrderBy(sp => sp.Servicio.SerNom)
                .Select(sp => new
                {
                    sp.SerID,
                    sp.Servicio.SerNom,
                    sp.Servicio.SerDesc
                })
                .ToListAsync();

            ViewBag.SerID = new SelectList(servicios, "SerID", "SerNom", serSel);
            ViewBag.ServiciosDetalle = servicios;
        }

        // 🔹 Cargar lista de vehículos por patente
        private async Task LoadVehiculos(string? selected = null)
        {
            var vehs = await _ctx.Vehiculos.AsNoTracking()
                .OrderBy(v => v.VehPtnt)
                .Select(v => v.VehPtnt)
                .ToListAsync();

            ViewBag.VehPtnt = new SelectList(vehs, selected);
            ViewBag.VehiculosList = vehs;
        }

        // 🔹 Validar que exista tarifa vigente para la combinación playa + servicio + tipo vehículo
        private async Task<bool> ValidarTarifaVigente(int plyID, int serID, int clasVehID)
        {
            var ahora = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            return await _ctx.TarifasServicio
                .Include(t => t.ServicioProveido)
                .AnyAsync(t =>
                    t.PlyID == plyID &&
                    t.SerID == serID &&
                    t.ClasVehID == clasVehID &&
                    (t.TasFecFin == null || t.TasFecFin > ahora) &&
                    t.TasFecIni <= ahora &&
                    t.ServicioProveido.SerProvHab);
        }

        // 🔹 Verificación asíncrona desde AJAX
        [HttpGet]
        public async Task<IActionResult> VerificarTarifaVigente(int plyID, int serID, int clasVehID)
        {
            var valido = await ValidarTarifaVigente(plyID, serID, clasVehID);
            return Json(valido);
        }

        // 🔹 Verificar si hay ocupación activa para la patente (AJAX)
        [HttpGet]
        public async Task<IActionResult> VerificarOcupacionActiva(int plyID, string vehPtnt)
        {
            if (string.IsNullOrWhiteSpace(vehPtnt))
                return Json(new { tieneOcupacion = false });

            var ocupacion = await _ctx.Ocupaciones
                .Include(o => o.Vehiculo)
                .FirstOrDefaultAsync(o => o.VehPtnt == vehPtnt &&
                                         o.PlyID == plyID &&
                                         o.OcufFyhFin == null);

            if (ocupacion == null || ocupacion.Vehiculo == null)
                return Json(new { tieneOcupacion = false });

            return Json(new { 
                tieneOcupacion = true,
                clasVehID = ocupacion.Vehiculo.ClasVehID
            });
        }

        // 🔹 GET: Create
        public async Task<IActionResult> Create(int? plyID = null)
        {
            SetBreadcrumb(
                new BreadcrumbItem { Title = "Servicios Extra", Url = Url.Action("Index", "ServicioExtraRealizado")! },
                new BreadcrumbItem { Title = "Registrar nuevo servicio", Url = Url.Action("Create", "ServicioExtraRealizado")! }
            );
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var plaNU))
                return BadRequest("ID de usuario inválido");

            // Turno activo del playero
            var turno = await _ctx.Turnos
                .FirstOrDefaultAsync(t => t.PlaNU == plaNU && t.TurFyhFin == null);

            if (turno == null)
                // Forzar la vista específica de este controller para evitar colisiones con otras
                // vistas "NoTurno" en otros controllers (p. ej. Ocupacion/NoTurno).
                return View("~/Views/ServicioExtraRealizado/NoTurno.cshtml");

            // Cargar servicios extra habilitados para la playa del turno
            await LoadServiciosHabilitados(turno.PlyID);

            // Obtener nombre de la playa
            var playaNombre = await _ctx.Playas
                .Where(p => p.PlyID == turno.PlyID)
                .Select(p => p.PlyNom)
                .FirstOrDefaultAsync();

            ViewBag.SelectedPlyID = turno.PlyID;
            ViewBag.SelectedPlyNombre = playaNombre;

            await LoadVehiculos();
            await LoadClasificacionesVehiculo();

            return View(new ServicioExtraRealizado
            {
                ServExFyHIni = DateTime.Now,
                PlyID = turno.PlyID
            });
        }

        // 🔹 POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServicioExtraRealizado model, int ClasVehID)
        {
            if (!ModelState.IsValid)
            {
                await LoadServiciosHabilitados(model.PlyID);
                await LoadVehiculos(model.VehPtnt);
                await LoadClasificacionesVehiculo(ClasVehID);
                
                // Verificar ocupación activa para mostrar en el badge
                if (!string.IsNullOrWhiteSpace(model.VehPtnt))
                {
                    var ocupacion = await _ctx.Ocupaciones
                        .Include(o => o.Vehiculo)
                        .FirstOrDefaultAsync(o => o.VehPtnt == model.VehPtnt &&
                                                  o.PlyID == model.PlyID &&
                                                  o.OcufFyhFin == null);
                    ViewBag.TieneOcupacionActiva = ocupacion != null;
                    if (ocupacion?.Vehiculo != null)
                    {
                        ViewBag.ClasVehIDOcupacion = ocupacion.Vehiculo.ClasVehID;
                    }
                }
                
                return View(model);
            }

            // Validar tarifa vigente
            var existeTarifa = await ValidarTarifaVigente(model.PlyID, model.SerID, ClasVehID);
            if (!existeTarifa)
            {
                ModelState.AddModelError("", "No existe una tarifa vigente para el servicio y tipo de vehículo seleccionados.");
                await LoadServiciosHabilitados(model.PlyID);
                await LoadVehiculos(model.VehPtnt);
                await LoadClasificacionesVehiculo(ClasVehID);
                
                // Verificar ocupación activa para mostrar en el badge
                if (!string.IsNullOrWhiteSpace(model.VehPtnt))
                {
                    var ocupacion = await _ctx.Ocupaciones
                        .Include(o => o.Vehiculo)
                        .FirstOrDefaultAsync(o => o.VehPtnt == model.VehPtnt &&
                                                  o.PlyID == model.PlyID &&
                                                  o.OcufFyhFin == null);
                    ViewBag.TieneOcupacionActiva = ocupacion != null;
                    if (ocupacion?.Vehiculo != null)
                    {
                        ViewBag.ClasVehIDOcupacion = ocupacion.Vehiculo.ClasVehID;
                    }
                }
                
                return View(model);
            }

            // 🔹 Verificar que el servicio provisto exista
            var existeServicio = await _ctx.ServiciosProveidos
                .AnyAsync(sp => sp.PlyID == model.PlyID && sp.SerID == model.SerID && sp.SerProvHab);

            if (!existeServicio)
            {
                ModelState.AddModelError("", "La playa no ofrece este servicio.");
                await LoadServiciosHabilitados(model.PlyID);
                await LoadVehiculos(model.VehPtnt);
                await LoadClasificacionesVehiculo(ClasVehID);
                
                // Verificar ocupación activa para mostrar en el badge
                if (!string.IsNullOrWhiteSpace(model.VehPtnt))
                {
                    var ocupacion = await _ctx.Ocupaciones
                        .Include(o => o.Vehiculo)
                        .FirstOrDefaultAsync(o => o.VehPtnt == model.VehPtnt &&
                                                  o.PlyID == model.PlyID &&
                                                  o.OcufFyhFin == null);
                    ViewBag.TieneOcupacionActiva = ocupacion != null;
                    if (ocupacion?.Vehiculo != null)
                    {
                        ViewBag.ClasVehIDOcupacion = ocupacion.Vehiculo.ClasVehID;
                    }
                }
                
                return View(model);
            }

            // 🔹 Verificar si existe ocupación activa para diferenciar escenarios de cobro:
            // - Con ocupación activa: cobrar estacionamiento + servicio extra
            // - Sin ocupación activa: cobrar solo servicio extra
            var ocupacionActiva = await _ctx.Ocupaciones
                .FirstOrDefaultAsync(o => o.VehPtnt == model.VehPtnt &&
                                         o.PlyID == model.PlyID &&
                                         o.OcufFyhFin == null);
            
            // La información de si hay o no ocupación activa estará disponible 
            // para la lógica de cobro posterior

            // 🔹 Verificar que el vehículo exista; si no, crear un registro mínimo con marca por defecto
            var existeVeh = await _ctx.Vehiculos.AnyAsync(v => v.VehPtnt == model.VehPtnt);
            if (!existeVeh)
            {
                var nuevoVeh = new Vehiculo
                {
                    VehPtnt = model.VehPtnt,
                    VehMarc = "No especificado",
                    ClasVehID = ClasVehID
                };

                try
                {
                    _ctx.Vehiculos.Add(nuevoVeh);
                    await _ctx.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"No se pudo crear el vehículo automáticamente: {ex.Message}");
                    await LoadServiciosHabilitados(model.PlyID);
                    await LoadVehiculos(model.VehPtnt);
                    await LoadClasificacionesVehiculo(ClasVehID);
                    return View(model);
                }
            }

            // 🔹 Definir estado inicial y hora
            model.ServExFyHIni = DateTime.UtcNow;
            model.ServExEstado = "Pendiente";

            // ⚠️ Importante: NO asignar objetos de navegación, solo las FK
            model.ServicioProveido = null;
            model.Vehiculo = null;

            try
            {
                model.ServExFyHIni = DateTime.UtcNow;
                model.ServExEstado = string.IsNullOrEmpty(model.ServExEstado) ? "Pendiente" : model.ServExEstado;

                _ctx.Entry(model).State = EntityState.Added;
                await _ctx.SaveChangesAsync();

                TempData["Saved"] = true;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al guardar el registro: {ex.Message}");
                await LoadServiciosHabilitados(model.PlyID);
                await LoadVehiculos(model.VehPtnt);
                await LoadClasificacionesVehiculo(ClasVehID);
                return View(model);
            }

        }

        // 🔹 INDEX: lista los servicios extra del turno activo del playero
        public async Task<IActionResult> Index()
        {
            SetBreadcrumb(
                new BreadcrumbItem { Title = "Servicios Extra", Url = Url.Action("Index", "ServicioExtraRealizado")! }
            );
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var plaNU))
                return BadRequest("ID de usuario inválido");

            // Buscar turno activo del playero
            var turno = await _ctx.Turnos
                .Include(t => t.Playa)
                .FirstOrDefaultAsync(t => t.PlaNU == plaNU && t.TurFyhFin == null);

            if (turno == null)
                return View("~/Views/ServicioExtraRealizado/NoTurno.cshtml");
            

            try
            {
                var lista = await _ctx.ServiciosExtrasRealizados
                    .Include(s => s.ServicioProveido).ThenInclude(sp => sp.Servicio)
                    .Include(s => s.Vehiculo)
                    .AsNoTracking()
                    .Where(s => s.PlyID == turno.PlyID)
                    .OrderByDescending(s => s.ServExFyHIni)
                    .ToListAsync();

                // Verificar ocupación activa para cada servicio extra
                var serviciosConOcupacion = new Dictionary<string, bool>();
                foreach (var servicio in lista)
                {
                    var key = $"{servicio.PlyID}_{servicio.SerID}_{servicio.VehPtnt}_{servicio.ServExFyHIni:o}";
                    var tieneOcupacion = await _ctx.Ocupaciones
                        .AnyAsync(o => o.VehPtnt == servicio.VehPtnt &&
                                      o.PlyID == servicio.PlyID &&
                                      o.OcufFyhFin == null);
                    serviciosConOcupacion[key] = tieneOcupacion;
                }

                ViewBag.PlayaNombre = turno.Playa.PlyNom;
                ViewBag.TurnoInicio = turno.TurFyhIni;
                ViewBag.ServiciosConOcupacion = serviciosConOcupacion;

                return View(lista);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error en Index: {ex.Message}");
                TempData["Error"] = $"Error al cargar los servicios extra: {ex.Message}";
                return View(new List<ServicioExtraRealizado>());
            }
        }

        // 🔹 GET: Detalles de un servicio extra realizado
        public async Task<IActionResult> Details(int plyID, int serID, string vehPtnt, DateTime servExFyHIni)
        {
            SetBreadcrumb(
                new BreadcrumbItem { Title = "Servicios Extra", Url = Url.Action("Index", "ServicioExtraRealizado")! },
                new BreadcrumbItem { Title = "Detalles", Url = Url.Action("Details", "ServicioExtraRealizado")! }
            );
            var item = await _ctx.ServiciosExtrasRealizados
                .Include(s => s.ServicioProveido).ThenInclude(sp => sp.Servicio)
                .Include(s => s.Vehiculo)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.PlyID == plyID && s.SerID == serID && s.VehPtnt == vehPtnt && s.ServExFyHIni == servExFyHIni);

            if (item == null) return NotFound();

            return View(item);
        }

        // 🔹 POST: Cambiar estado de un servicio extra (Playero)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int plyID, int serID, string vehPtnt, DateTime servExFyHIni, string nuevoEstado)
        {
            // Verificar turno activo del playero y que corresponda a la misma playa
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var plaNU))
                return BadRequest("ID de usuario inválido");

            var turno = await _ctx.Turnos.FirstOrDefaultAsync(t => t.PlaNU == plaNU && t.TurFyhFin == null);
            if (turno == null || turno.PlyID != plyID)
                return Forbid();

            var key = new object[] { plyID, serID, vehPtnt, servExFyHIni };
            var item = await _ctx.ServiciosExtrasRealizados.FindAsync(key);
            if (item == null)
                return NotFound();

            // Sólo permitir transiciones conocidas
            if (nuevoEstado == "En curso")
            {
                item.ServExEstado = "En curso";
            }
            else if (nuevoEstado == "Finalizado" || nuevoEstado == "Completado")
            {
                item.ServExEstado = "Completado";
                item.ServExFyHFin = DateTime.UtcNow;
            }
            else
            {
                return BadRequest("Estado no permitido");
            }

            try
            {
                _ctx.Entry(item).State = EntityState.Modified;
                await _ctx.SaveChangesAsync();
                TempData["Saved"] = true;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"No se pudo cambiar el estado: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // 🔹 GET: Retirar servicio extra (solo para servicios sin ocupación activa)
        [HttpGet]
        public async Task<IActionResult> Retirar(int plyID, int serID, string vehPtnt, DateTime servExFyHIni)
        {
            // Verificar turno activo del playero
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var plaNU))
                return BadRequest("ID de usuario inválido");

            var turno = await _ctx.Turnos.FirstOrDefaultAsync(t => t.PlaNU == plaNU && t.TurFyhFin == null);
            if (turno == null || turno.PlyID != plyID)
                return Forbid();

            // Buscar el servicio extra
            var servicio = await _ctx.ServiciosExtrasRealizados
                .Include(s => s.ServicioProveido).ThenInclude(sp => sp.Servicio)
                .Include(s => s.Vehiculo).ThenInclude(v => v.Clasificacion)
                .FirstOrDefaultAsync(s => s.PlyID == plyID && 
                                         s.SerID == serID && 
                                         s.VehPtnt == vehPtnt && 
                                         s.ServExFyHIni == servExFyHIni);

            if (servicio == null)
                return NotFound();

            // Verificar que no tenga ocupación activa
            var tieneOcupacion = await _ctx.Ocupaciones
                .AnyAsync(o => o.VehPtnt == vehPtnt &&
                              o.PlyID == plyID &&
                              o.OcufFyhFin == null);

            if (tieneOcupacion)
            {
                TempData["Error"] = "Este servicio extra se cobrará junto con la ocupación al momento del egreso.";
                return RedirectToAction(nameof(Index));
            }

            // Verificar que el servicio esté completado
            if (servicio.ServExEstado != "Completado")
            {
                TempData["Error"] = "El servicio debe estar completado para poder retirar.";
                return RedirectToAction(nameof(Index));
            }

            // Verificar que no haya sido retirado ya (PagNum != null)
            if (servicio.PagNum != null)
            {
                TempData["Error"] = "Este servicio ya ha sido retirado y cobrado.";
                return RedirectToAction(nameof(Index));
            }

            // Calcular el cobro
            var fechaRetiro = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            var cobroVM = await CalcularCobroServicioExtra(plyID, serID, vehPtnt, servExFyHIni, fechaRetiro);

            return View("CobroServicioExtra", cobroVM);
        }

        // 🔹 Calcular cobro para servicio extra sin ocupación activa
        private async Task<CobroServicioExtraVM> CalcularCobroServicioExtra(
            int plyID, int serID, string vehPtnt, DateTime servExFyHIni, DateTime servExFyHFin)
        {
            var servicio = await _ctx.ServiciosExtrasRealizados
                .Include(s => s.ServicioProveido).ThenInclude(sp => sp.Servicio)
                .Include(s => s.Vehiculo).ThenInclude(v => v.Clasificacion)
                .FirstOrDefaultAsync(s => s.PlyID == plyID &&
                                         s.SerID == serID &&
                                         s.VehPtnt == vehPtnt &&
                                         s.ServExFyHIni == servExFyHIni);

            if (servicio == null || servicio.Vehiculo == null)
                throw new InvalidOperationException("Servicio extra o vehículo no encontrado");

            var playa = await _ctx.Playas.FindAsync(plyID);
            if (playa == null)
                throw new InvalidOperationException("Playa no encontrada");

            // Buscar tarifa vigente para el servicio extra y tipo de vehículo
            var ahora = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            var tarifa = await _ctx.TarifasServicio
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Servicio)
                .Where(t => t.PlyID == plyID &&
                           t.SerID == serID &&
                           t.ClasVehID == servicio.Vehiculo.ClasVehID &&
                           (t.TasFecFin == null || t.TasFecFin > ahora) &&
                           t.TasFecIni <= ahora &&
                           t.ServicioProveido.SerProvHab)
                .OrderByDescending(t => t.TasFecIni)
                .FirstOrDefaultAsync();

            if (tarifa == null)
                throw new InvalidOperationException("No se encontró una tarifa vigente para este servicio y tipo de vehículo");

            var serviciosAplicables = new List<ServicioCobroVM>
            {
                new ServicioCobroVM
                {
                    SerID = serID,
                    SerNom = servicio.ServicioProveido!.Servicio!.SerNom,
                    SerTipo = servicio.ServicioProveido.Servicio.SerTipo,
                    TarifaVigente = tarifa.TasMonto,
                    Cantidad = 1,
                    Subtotal = tarifa.TasMonto,
                    EsEstacionamiento = false
                }
            };

            // Métodos de pago
            var metodosPago = await _ctx.AceptaMetodosPago
                .Include(amp => amp.MetodoPago)
                .Where(amp => amp.PlyID == plyID && amp.AmpHab)
                .Select(amp => new MetodoPagoVM
                {
                    MepID = amp.MepID,
                    MepNom = amp.MetodoPago!.MepNom,
                    MepDesc = amp.MetodoPago!.MepDesc
                })
                .ToListAsync();

            return new CobroServicioExtraVM
            {
                PlyID = plyID,
                SerID = serID,
                VehPtnt = vehPtnt,
                ServExFyHIni = servExFyHIni,
                ServExFyHFin = servExFyHFin,
                ClasVehID = servicio.Vehiculo.ClasVehID,
                ClasVehTipo = servicio.Vehiculo.Clasificacion?.ClasVehTipo ?? "",
                PlayaNombre = playa.PlyNom,
                ServicioNombre = servicio.ServicioProveido!.Servicio!.SerNom,
                ServiciosAplicables = serviciosAplicables,
                TotalCobro = tarifa.TasMonto,
                MetodosPagoDisponibles = metodosPago
            };
        }

        // 🔹 POST: Confirmar retiro y cobro de servicio extra
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarRetiro(CobroServicioExtraVM model)
        {
            // Verificar turno activo del playero
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var plaNU))
                return BadRequest("ID de usuario inválido");

            var turno = await _ctx.Turnos.FirstOrDefaultAsync(t => t.PlaNU == plaNU && t.TurFyhFin == null);
            if (turno == null || turno.PlyID != model.PlyID)
                return Forbid();

            if (!ModelState.IsValid)
            {
                // Recargar el modelo con los datos originales
                var cobroVM = await CalcularCobroServicioExtra(model.PlyID, model.SerID, model.VehPtnt,
                    model.ServExFyHIni, model.ServExFyHFin);
                cobroVM.MepID = model.MepID; // Mantener la selección del usuario
                return View("CobroServicioExtra", cobroVM);
            }

            // Buscar el servicio extra
            var servicio = await _ctx.ServiciosExtrasRealizados
                .FirstOrDefaultAsync(s => s.PlyID == model.PlyID &&
                                         s.SerID == model.SerID &&
                                         s.VehPtnt == model.VehPtnt &&
                                         s.ServExFyHIni == model.ServExFyHIni);

            if (servicio == null)
            {
                TempData["Error"] = "No se encontró el servicio especificado.";
                return RedirectToAction(nameof(Index));
            }

            // Verificar que no tenga ocupación activa
            var tieneOcupacion = await _ctx.Ocupaciones
                .AnyAsync(o => o.VehPtnt == model.VehPtnt &&
                              o.PlyID == model.PlyID &&
                              o.OcufFyhFin == null);

            if (tieneOcupacion)
            {
                TempData["Error"] = "Este servicio extra se cobrará junto con la ocupación al momento del egreso.";
                return RedirectToAction(nameof(Index));
            }

            // Verificar que no haya sido retirado ya
            if (servicio.PagNum != null)
            {
                TempData["Error"] = "Este servicio ya ha sido retirado y cobrado.";
                return RedirectToAction(nameof(Index));
            }

            await using var tx = await _ctx.Database.BeginTransactionAsync();

            try
            {
                // Crear el pago
                var proximoNumeroPago = await _ctx.Pagos
                    .Where(p => p.PlyID == model.PlyID)
                    .MaxAsync(p => (int?)p.PagNum) + 1 ?? 1;

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

                // Actualizar el servicio extra con el pago
                servicio.PagNum = pago.PagNum;
                servicio.ServExFyHFin = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                _ctx.ServiciosExtrasRealizados.Update(servicio);
                await _ctx.SaveChangesAsync();

                await tx.CommitAsync();

                TempData["Success"] = $"Servicio extra retirado y cobrado exitosamente. Total: ${model.TotalCobro:F2}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = $"Error al procesar el retiro: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

    }
}