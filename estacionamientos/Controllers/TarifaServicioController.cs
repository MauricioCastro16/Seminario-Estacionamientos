using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.ViewModels;
using estacionamientos.Models.ViewModels;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace estacionamientos.Controllers
{
    [Authorize(Roles = "Duenio,Playero")]
    public class TarifaServicioController : Controller
    {
        private readonly AppDbContext _ctx;
        public TarifaServicioController(AppDbContext ctx) => _ctx = ctx;


        private async Task LoadSelects(int? plySel = null, int? serSel = null, int? clasSel = null)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = int.Parse(userIdStr!);

            var playas = await _ctx.Playas
                .AsNoTracking()
                .Where(p => p.Administradores.Any(a => a.Duenio.UsuNU == userId))
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyNom })
                .ToListAsync();

            List<Servicio> servicios = new();
            if (plySel.HasValue && plySel.Value > 0)
            {
                servicios = await _ctx.ServiciosProveidos
                    .AsNoTracking()
                    .Where(sp => sp.PlyID == plySel.Value && sp.SerProvHab)
                    .Select(sp => sp.Servicio)
                    .OrderBy(s => s.SerNom)
                    .ToListAsync();
            }

            var clases = await _ctx.ClasificacionesVehiculo
                .AsNoTracking()
                .OrderBy(c => c.ClasVehTipo)
                .ToListAsync();

            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", plySel);
            ViewBag.SerID = new SelectList(servicios, "SerID", "SerNom", serSel);
            ViewBag.ClasVehID = new SelectList(clases, "ClasVehID", "ClasVehTipo", clasSel);
        }



        // Helper: normaliza DateTime a UTC
        private DateTime ToUtc(DateTime dt) => DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        // Helper: normaliza texto removiendo acentos y convirtiendo a minúsculas
        private string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            
            return text.ToLowerInvariant()
                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
                .Replace("à", "a").Replace("è", "e").Replace("ì", "i").Replace("ò", "o").Replace("ù", "u")
                .Replace("ä", "a").Replace("ë", "e").Replace("ï", "i").Replace("ö", "o").Replace("ü", "u")
                .Replace("â", "a").Replace("ê", "e").Replace("î", "i").Replace("ô", "o").Replace("û", "u")
                .Replace("ã", "a").Replace("ẽ", "e").Replace("ĩ", "i").Replace("õ", "o").Replace("ũ", "u")
                .Replace("ç", "c").Replace("ñ", "n");
        }

        // INDEX
        [Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Index(
            string q,
            string filterBy = "todos",
            List<string>? Servicios = null,
            List<string>? Clases = null,
            List<string>? Todos = null,
            List<string>? Montos = null,
            List<string>? FechasDesde = null,
            List<string>? FechasHasta = null,
            string? selectedOption = null,
            string? remove = null,
            int? plyID = null) 
        {
            var tarifas = _ctx.TarifasServicio
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Playa)
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Servicio)
                .Include(t => t.ClasificacionVehiculo)
                .AsQueryable();

            if (plyID.HasValue)
            {
                tarifas = tarifas.Where(t => t.PlyID == plyID.Value);

                // Traer el nombre de la playa para mostrarlo en la vista
                var playa = await _ctx.Playas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PlyID == plyID.Value);

                if (playa != null)
                    ViewBag.PlayaNombre = playa.PlyNom;
            }

            // quitar un filtro (igual que en Playas)
            if (!string.IsNullOrEmpty(remove))
            {
                var parts = remove.Split(':');
                if (parts.Length >= 2)
                {
                    var key = parts[0].ToLower();
                    var val = parts[1];

                    if (key == "servicio") Servicios?.Remove(val);
                    if (key == "clase") Clases?.Remove(val);
                    if (key == "todos") Todos?.Remove(val);
                    if (key == "monto") Montos?.Remove(val);
                    if (key == "fechas" && parts.Length == 3)
                    {
                        var fechaDesde = parts[1];
                        var fechaHasta = parts[2];
                        FechasDesde?.Remove(fechaDesde);
                        FechasHasta?.Remove(fechaHasta);
                    }
                }
            }

            // aplicar búsqueda principal

            // Búsqueda principal - usar EF.Functions para búsquedas que EF puede traducir
            if (!string.IsNullOrWhiteSpace(q))
            {
                tarifas = filterBy switch
                {
                    "servicio" => tarifas.Where(t => t.ServicioProveido.Servicio.SerNom.ToLower().Contains(q.ToLower())),
                    "clase" => tarifas.Where(t => t.ClasificacionVehiculo.ClasVehTipo.ToLower().Contains(q.ToLower())),
                    "monto" => tarifas.Where(t => t.TasMonto.ToString().Contains(q)),
                    _ => tarifas.Where(t =>
                        t.ServicioProveido.Servicio.SerNom.ToLower().Contains(q.ToLower()) ||
                        t.ClasificacionVehiculo.ClasVehTipo.ToLower().Contains(q.ToLower()) ||
                        t.TasMonto.ToString().Contains(q))
                };
            }

            // Búsqueda por rango de fechas (cuando filterBy es "fechas")
            if (filterBy == "fechas")
            {
                DateTime? fechaDesde = null;
                DateTime? fechaHasta = null;

                // Parsear fecha desde
                if (!string.IsNullOrWhiteSpace(selectedOption) && DateTime.TryParse(selectedOption, out var fd))
                {
                    fechaDesde = ToUtc(fd);
                }

                // Parsear fecha hasta desde el parámetro FechaHasta
                if (!string.IsNullOrWhiteSpace(Request.Query["FechaHasta"].FirstOrDefault()) && 
                    DateTime.TryParse(Request.Query["FechaHasta"].FirstOrDefault(), out var fh))
                {
                    fechaHasta = ToUtc(fh);
                }

                // Aplicar filtros según el rango de fechas
                if (fechaDesde.HasValue && fechaHasta.HasValue)
                {
                    // Rango completo: buscar tarifas que estuvieron activas durante el período del filtro
                    tarifas = tarifas.Where(t => 
                        // Tarifa sin fecha fin (vigente): debe empezar dentro del rango del filtro
                        (t.TasFecFin == null && t.TasFecIni >= fechaDesde.Value && t.TasFecIni <= fechaHasta.Value) ||
                        // Tarifa con fecha fin: debe estar completamente dentro del rango del filtro
                        (t.TasFecFin != null && t.TasFecIni >= fechaDesde.Value && t.TasFecFin <= fechaHasta.Value));
                }
                else if (fechaDesde.HasValue)
                {
                    // Solo fecha desde: buscar tarifas que empezaron después de esa fecha
                    tarifas = tarifas.Where(t => t.TasFecIni >= fechaDesde.Value);
                }
                else if (fechaHasta.HasValue)
                {
                    // Solo fecha hasta: buscar tarifas que empezaron antes de esa fecha
                    tarifas = tarifas.Where(t => t.TasFecIni <= fechaHasta.Value);
                }
            }


            // filtros acumulados
            if (Servicios?.Any() ?? false)
            {
                tarifas = tarifas.Where(t => 
                    Servicios.Any(servicio => 
                        t.ServicioProveido.Servicio.SerNom.ToLower().Contains(servicio.ToLower())));
            }

            if (Clases?.Any() ?? false)
            {
                tarifas = tarifas.Where(t => 
                    Clases.Any(clase => 
                        t.ClasificacionVehiculo.ClasVehTipo.ToLower().Contains(clase.ToLower())));
            }

            if (Montos?.Any() ?? false)
            {
                var montos = Montos.Select(m => decimal.Parse(m)).ToList();
                tarifas = tarifas.Where(t => montos.Contains(t.TasMonto));
            }

            if (Todos?.Any() ?? false)
            {
                // Para "Todos", buscar en todos los campos (servicio, clase, monto)
                // Cada término debe coincidir en AL MENOS UN campo (OR entre campos)
                tarifas = tarifas.Where(t => 
                    Todos.Any(term => 
                        t.ServicioProveido.Servicio.SerNom.ToLower().Contains(term.ToLower()) ||
                        t.ClasificacionVehiculo.ClasVehTipo.ToLower().Contains(term.ToLower()) ||
                        t.TasMonto.ToString().Contains(term)));
            }

            if (FechasDesde?.Any() ?? false)
            {
                for (int i = 0; i < FechasDesde.Count; i++)
                {
                    if (DateTime.TryParse(FechasDesde[i], out var fechaDesde))
                    {
                        fechaDesde = ToUtc(fechaDesde);
                        
                        // Si hay fecha hasta correspondiente, usar rango; si no, solo fecha desde
                        if (FechasHasta != null && i < FechasHasta.Count && DateTime.TryParse(FechasHasta[i], out var fechaHasta))
                        {
                            fechaHasta = ToUtc(fechaHasta);
                            tarifas = tarifas.Where(t => t.TasFecIni >= fechaDesde && t.TasFecIni <= fechaHasta);
                        }
                        else
                        {
                            // Solo fecha de inicio: buscar tarifas que empezaron después de esa fecha
                            tarifas = tarifas.Where(t => t.TasFecIni >= fechaDesde);
                        }
                    }
                }
            }

            tarifas = tarifas.Where(t =>
                (t.TasFecFin == null || t.TasFecFin > DateTime.UtcNow) &&
                t.ServicioProveido.SerProvHab);

            var lista = await tarifas
                .AsNoTracking()
                .OrderBy(t => t.ServicioProveido.Servicio.SerNom)
                .ThenBy(t => t.ClasificacionVehiculo.ClasVehTipo)
                .ToListAsync();



            var vm = new TarifasIndexVM
            {
                Tarifas = lista,
                Q = q ?? "",
                FilterBy = string.IsNullOrEmpty(filterBy) ? "todos" : filterBy.ToLower(),
                Servicios = Servicios ?? new(),
                Clases = Clases ?? new(),
                Todos = Todos ?? new(),
                Montos = Montos ?? new(),
                FechasDesde = FechasDesde ?? new(),
                FechasHasta = FechasHasta ?? new(),
                SelectedOption = selectedOption
            };
            // Pasamos el plyID a la vista para usarlo en el botón "Nueva Tarifa"
            ViewBag.PlyID = plyID;

            return View(vm);
        }

        // DETAILS
        [Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Details(int plyID, int serID, int clasVehID, DateTime tasFecIni)
        {
            tasFecIni = ToUtc(tasFecIni);

            var item = await _ctx.TarifasServicio
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Servicio)
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Playa)
                .Include(t => t.ClasificacionVehiculo)
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.PlyID == plyID &&
                    t.SerID == serID &&
                    t.ClasVehID == clasVehID &&
                    t.TasFecIni == tasFecIni);

            return item is null ? NotFound() : View(item);
        }

        // CREATE GET
        [Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Create(int? plySel = null)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = int.Parse(userIdStr!);

            // 👇 obtener la primera playa del dueño si todavía no se eligió ninguna
            if (!plySel.HasValue)
            {
                plySel = await _ctx.Playas
                    .Where(p => p.Administradores.Any(a => a.Duenio.UsuNU == userId))
                    .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                    .Select(p => (int?)p.PlyID)
                    .FirstOrDefaultAsync();
            }

            await LoadSelects(plySel);

            var playa = await _ctx.Playas
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlyID == plySel);

            ViewBag.PlayaNombre = playa?.PlyNom ?? "";


            return View(new TarifaServicio
            {
                TasFecIni = DateTime.Today,
                PlyID = plySel ?? 0
            });
        }


        // CREATE POST
        [Authorize(Roles = "Duenio")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TarifaServicio model)
        {
            try
            {
                model.TasFecIni = ToUtc(model.TasFecIni);
                if (model.TasFecFin.HasValue)
                    model.TasFecFin = ToUtc(model.TasFecFin.Value);


                // 🔴 Validación: monto debe ser > 0
                if (model.TasMonto <= 0)
                    ModelState.AddModelError("TasMonto", "El monto debe ser mayor a 0.");

                var existeSP = await _ctx.ServiciosProveidos
                    .AnyAsync(sp => sp.PlyID == model.PlyID &&
                                    sp.SerID == model.SerID &&
                                    sp.SerProvHab);

                if (!existeSP)
                    ModelState.AddModelError("", "La playa no ofrece ese servicio.");

                var vigente = await _ctx.TarifasServicio
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t =>
                        t.PlyID == model.PlyID &&
                        t.SerID == model.SerID &&
                        t.ClasVehID == model.ClasVehID &&
                        t.TasFecFin == null);

                if (vigente != null)
                    ModelState.AddModelError("", "Ya existe una tarifa vigente para esta playa, servicio y clase de vehículo.");

                if (!ModelState.IsValid)
                {
                    await LoadSelects(model.PlyID, model.SerID, model.ClasVehID);

                    // 🔴 Recuperar nombre de la playa para que no se borre
                    ViewBag.PlayaNombre = await _ctx.Playas
                        .Where(p => p.PlyID == model.PlyID)
                        .Select(p => p.PlyNom)
                        .FirstOrDefaultAsync();

                    return View(model);
                }

                model.TasFecFin = null;
                _ctx.TarifasServicio.Add(model);
                await _ctx.SaveChangesAsync();

                TempData["Saved"] = true; // 👈 bandera para JS


                if (TempData["Saved"] != null)
                {
                    return RedirectToAction(nameof(Index), new { plyID = model.PlyID });
                }
                else
                {
                    return RedirectToAction(nameof(Create), new { plySel = model.PlyID }); // Redirigir a Create con plySel
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                await LoadSelects(model.PlyID, model.SerID, model.ClasVehID);

                ViewBag.PlayaNombre = await _ctx.Playas
                    .Where(p => p.PlyID == model.PlyID)
                    .Select(p => p.PlyNom)
                    .FirstOrDefaultAsync();

                return View(model);
            }
        }

        // EDIT GET
        [Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Edit(int plyID, int serID, int clasVehID, DateTime tasFecIni)
        {
            tasFecIni = ToUtc(tasFecIni);
            // Buscar la tarifa vigente actual para esta combinación de playa, servicio y clase de vehículo
            var item = await _ctx.TarifasServicio
                .Where(t => t.PlyID == plyID && 
                           t.SerID == serID && 
                           t.ClasVehID == clasVehID &&
                           (t.TasFecFin == null || t.TasFecFin > DateTime.UtcNow))
                .OrderByDescending(t => t.TasFecIni) // Obtener la más reciente
                .FirstOrDefaultAsync();
            
            if (item is null) return NotFound();

            await LoadSelects(item.PlyID, item.SerID, item.ClasVehID);
            return View(item);
        }

        // EDIT POST
        [Authorize(Roles = "Duenio")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int serID, int clasVehID, DateTime tasFecIni, TarifaServicio model)
        {
            tasFecIni = ToUtc(tasFecIni);

            // Validar que los IDs coincidan
            if (plyID != model.PlyID || serID != model.SerID || clasVehID != model.ClasVehID)
                return BadRequest();

            model.TasFecIni = ToUtc(model.TasFecIni);
            if (model.TasFecFin.HasValue)
                model.TasFecFin = ToUtc(model.TasFecFin.Value);

            // 🔴 Validación: monto debe ser > 0
            if (model.TasMonto <= 0)
                ModelState.AddModelError("TasMonto", "El monto debe ser mayor a 0.");

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.SerID, model.ClasVehID);
                return View(model);
            }

            // Obtener la tarifa vigente actual para esta combinación
            var tarifaVigente = await _ctx.TarifasServicio
                .Where(t => t.PlyID == plyID && 
                           t.SerID == serID && 
                           t.ClasVehID == clasVehID &&
                           (t.TasFecFin == null || t.TasFecFin > DateTime.UtcNow))
                .OrderByDescending(t => t.TasFecIni)
                .FirstOrDefaultAsync();

            if (tarifaVigente == null)
                return NotFound();

            // Si el monto cambió, cerrar la tarifa actual y crear una nueva
            if (tarifaVigente.TasMonto != model.TasMonto)
            {
                // Cerrar la tarifa vigente actual
                tarifaVigente.TasFecFin = ToUtc(DateTime.Now);
                _ctx.Update(tarifaVigente);

                // Crear nueva tarifa con el monto actualizado
                var nuevaTarifa = new TarifaServicio
                {
                    PlyID = model.PlyID,
                    SerID = model.SerID,
                    ClasVehID = model.ClasVehID,
                    TasMonto = model.TasMonto,
                    TasFecIni = ToUtc(DateTime.Now),
                    TasFecFin = model.TasFecFin // Preservar la fecha de fin de vigencia del formulario
                };

                _ctx.TarifasServicio.Add(nuevaTarifa);
                await _ctx.SaveChangesAsync();

                TempData["Saved"] = true;
                return RedirectToAction(nameof(Index), new { plyID = model.PlyID });
            }
            else
            {
                // Si el monto no cambió, solo actualizar la fecha de fin si se proporcionó
                if (model.TasFecFin != null)
                {
                    bool solapa = await _ctx.TarifasServicio.AnyAsync(t =>
                        t.PlyID == model.PlyID &&
                        t.SerID == model.SerID &&
                        t.ClasVehID == model.ClasVehID &&
                        t.TasFecIni > tarifaVigente.TasFecIni &&
                        (t.TasFecFin == null || t.TasFecFin > model.TasFecFin));

                    if (solapa)
                    {
                        ModelState.AddModelError("", "Las fechas ingresadas generan solapamiento con otra tarifa.");
                        await LoadSelects(model.PlyID, model.SerID, model.ClasVehID);
                        return View(model);
                    }
                }

                // Actualizar la tarifa vigente con los nuevos datos
                tarifaVigente.TasFecFin = model.TasFecFin;
                _ctx.Update(tarifaVigente);
                await _ctx.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { plyID = model.PlyID });
            }
        }

        // DELETE POST
        [Authorize(Roles = "Duenio")]
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int serID, int clasVehID, DateTime tasFecIni)
        {
            tasFecIni = ToUtc(tasFecIni);

            var item = await _ctx.TarifasServicio.FindAsync(plyID, serID, clasVehID, tasFecIni);
            if (item is null) return NotFound();

            // 🔴 Forzar cierre siempre, aunque ya tenga TasFecFin
            item.TasFecFin = ToUtc(DateTime.Now);

            _ctx.Update(item);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { plyID = plyID });
        }

        [Authorize(Roles = "Playero")]
        public async Task<IActionResult> VigentesPlayero(int plyId)
        {
            var ahora = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);

            var playa = await _ctx.Playas
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlyID == plyId);

            if (playa == null) return NotFound();

            var tarifas = await _ctx.TarifasServicio
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Servicio)
                .Include(t => t.ClasificacionVehiculo)
                .Where(t => t.PlyID == plyId &&
                        (t.TasFecFin == null || t.TasFecFin > ahora) &&
                        t.ServicioProveido.SerProvHab)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Playa = playa;
            return View(tarifas);
        }
        
        [Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Historial(int? plyID = null)
        {
            var query = _ctx.TarifasServicio
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Servicio)
                .Include(t => t.ClasificacionVehiculo)
                .AsNoTracking()
                .AsQueryable();

            if (plyID.HasValue)
            {
                query = query.Where(t => t.PlyID == plyID.Value);

                var playa = await _ctx.Playas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PlyID == plyID.Value);

                if (playa != null)
                    ViewBag.PlayaNombre = playa.PlyNom;
                    ViewBag.PlyID = plyID.Value;   // 👈 esto es lo que le faltaba

            }

            var tarifas = await query.ToListAsync();

            var grupos = tarifas
                .GroupBy(t => t.ServicioProveido.Servicio.SerNom)
                .Select(g => new TarifaHistGroupVM
                {
                    ServicioNombre = g.Key,
                    Periodos = g.Select(t => new TarifaPeriodoVM
                    {
                        ClaseVehiculo = t.ClasificacionVehiculo.ClasVehTipo,
                        Monto = t.TasMonto,
                        FechaInicio = t.TasFecIni,
                        FechaFin = t.TasFecFin
                    })
                    .OrderBy(p => p.ClaseVehiculo)
                    .ToList()
                })
                .OrderBy(g => g.ServicioNombre)
                .ToList();

            ViewData["PlyID"] = plyID;
            
            return View(grupos);
        }

   

    }
}
