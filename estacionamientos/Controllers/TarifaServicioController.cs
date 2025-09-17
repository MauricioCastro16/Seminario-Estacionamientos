using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.Models.ViewModels;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace estacionamientos.Controllers
{
    [Authorize(Roles = "Duenio")]

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
       
        // INDEX
        public async Task<IActionResult> Index(
            string q,
            string filterBy = "all",
            List<string>? Playas = null,
            List<string>? Servicios = null,
            List<string>? Clases = null,
            List<string>? Vigencias = null,
            List<string>? Todos = null,
            string? selectedOption = null,   // 👈 ahora queda antes
            string? remove = null)           // 👈 último
        {
            var tarifas = _ctx.TarifasServicio
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Playa)
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Servicio)
                .Include(t => t.ClasificacionVehiculo)
                .AsQueryable();

            // quitar un filtro (igual que en Playas)
            if (!string.IsNullOrEmpty(remove))
            {
                var parts = remove.Split(':');
                if (parts.Length == 2)
                {
                    var key = parts[0].ToLower();
                    var val = parts[1];

                    if (key == "playa") Playas?.Remove(val);
                    if (key == "servicio") Servicios?.Remove(val);
                    if (key == "clase") Clases?.Remove(val);
                    if (key == "vigencia") Vigencias?.Remove(val);
                    if (key == "todos") Todos?.Remove(val);
                }
            }

            // 👇 trasladar la opción elegida en el combo de vigencia (normalizamos a lower)
            if (!string.IsNullOrWhiteSpace(selectedOption) && filterBy == "vigencia")
            {
                Vigencias = new List<string> { selectedOption.ToLower() };
            }

            // aplicar búsqueda principal (q)
            if (!string.IsNullOrWhiteSpace(q))
            {
                var qLower = q.ToLower();
                tarifas = filterBy switch
                {
                    "playa" => tarifas.Where(t => t.ServicioProveido.Playa.PlyNom.ToLower().Contains(qLower)),
                    "servicio" => tarifas.Where(t => t.ServicioProveido.Servicio.SerNom.ToLower().Contains(qLower)),
                    "clase" => tarifas.Where(t => t.ClasificacionVehiculo.ClasVehTipo.ToLower().Contains(qLower)),
                    // 👇 búsqueda textual por vigencia
                    "vigencia" => qLower switch
                    {
                        "vigente" => tarifas.Where(t => t.TasFecFin == null || t.TasFecFin > DateTime.UtcNow),
                        "no vigente" => tarifas.Where(t => t.TasFecFin != null && t.TasFecFin <= DateTime.UtcNow),
                        _ => tarifas
                    },
                    _ => tarifas.Where(t =>
                        t.ServicioProveido.Playa.PlyNom.ToLower().Contains(qLower) ||
                        t.ServicioProveido.Servicio.SerNom.ToLower().Contains(qLower) ||
                        t.ClasificacionVehiculo.ClasVehTipo.ToLower().Contains(qLower))
                };
            }

            // filtros acumulados
            if (Playas?.Any() ?? false)
                tarifas = tarifas.Where(t => Playas
                    .Any(p => p.Equals(t.ServicioProveido.Playa.PlyNom, StringComparison.OrdinalIgnoreCase)));

            if (Servicios?.Any() ?? false)
                tarifas = tarifas.Where(t => Servicios
                    .Any(s => s.Equals(t.ServicioProveido.Servicio.SerNom, StringComparison.OrdinalIgnoreCase)));

            if (Clases?.Any() ?? false)
                tarifas = tarifas.Where(t => Clases
                    .Any(c => c.Equals(t.ClasificacionVehiculo.ClasVehTipo, StringComparison.OrdinalIgnoreCase)));

            if (Vigencias?.Any() ?? false)
            {
                var ahora = DateTime.UtcNow;
                if (Vigencias.Any(v => v.Equals("vigente", StringComparison.OrdinalIgnoreCase)))
                    tarifas = tarifas.Where(t => t.TasFecFin == null || t.TasFecFin > ahora);

                if (Vigencias.Any(v => v.Equals("no vigente", StringComparison.OrdinalIgnoreCase)))
                    tarifas = tarifas.Where(t => t.TasFecFin != null && t.TasFecFin <= ahora);
            }

            var vm = new TarifasIndexVM
            {
                Tarifas = await tarifas.AsNoTracking().ToListAsync(),
                Q = q ?? "",
                FilterBy = filterBy,
                Playas = Playas ?? new(),
                Servicios = Servicios ?? new(),
                Clases = Clases ?? new(),
                Vigencias = Vigencias ?? new(),
                Todos = Todos ?? new(),
                SelectedOption = selectedOption
            };

            return View(vm);
        }

        // DETAILS
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

            return View(new TarifaServicio
            {
                TasFecIni = DateTime.Today,
                PlyID = plySel ?? 0
            });
        }


        // CREATE POST
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
                    .AnyAsync(sp => sp.PlyID == model.PlyID && sp.SerID == model.SerID);

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
                    return View(model);
                }

                model.TasFecFin = null;
                _ctx.TarifasServicio.Add(model);
                await _ctx.SaveChangesAsync();

                TempData["Saved"] = true; // 👈 bandera para JS


                if (TempData["Saved"] != null)
                {
                    return RedirectToAction("Index", "TarifaServicio"); // Redirigir a la acción Index de TarifaServicio
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
                return View(model);
            }
        }


        // EDIT GET
        public async Task<IActionResult> Edit(int plyID, int serID, int clasVehID, DateTime tasFecIni)
        {
            tasFecIni = ToUtc(tasFecIni);

            var item = await _ctx.TarifasServicio.FindAsync(plyID, serID, clasVehID, tasFecIni);
            if (item is null) return NotFound();

            await LoadSelects(item.PlyID, item.SerID, item.ClasVehID);
            return View(item);
        }

        // EDIT POST
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int serID, int clasVehID, DateTime tasFecIni, TarifaServicio model)
        {
            tasFecIni = ToUtc(tasFecIni);

            if (plyID != model.PlyID || serID != model.SerID || clasVehID != model.ClasVehID || tasFecIni != ToUtc(model.TasFecIni))
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

            if (model.TasFecFin != null)
            {
                bool solapa = await _ctx.TarifasServicio.AnyAsync(t =>
                    t.PlyID == model.PlyID &&
                    t.SerID == model.SerID &&
                    t.ClasVehID == model.ClasVehID &&
                    t.TasFecIni > model.TasFecIni &&
                    (t.TasFecFin == null || t.TasFecFin > model.TasFecFin));

                if (solapa)
                {
                    ModelState.AddModelError("", "Las fechas ingresadas generan solapamiento con otra tarifa.");
                    await LoadSelects(model.PlyID, model.SerID, model.ClasVehID);
                    return View(model);
                }
            }

            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // DELETE GET
        public async Task<IActionResult> Delete(int plyID, int serID, int clasVehID, DateTime tasFecIni)
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

        // DELETE POST
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

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> VigentesPlayero(int plyId)
        {
            var ahora = DateTime.UtcNow;

            var playa = await _ctx.Playas
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlyID == plyId);

            if (playa == null) return NotFound();

            var tarifas = await _ctx.TarifasServicio
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Servicio)
                .Include(t => t.ClasificacionVehiculo)
                .Where(t => t.PlyID == plyId &&
                            (t.TasFecFin == null || t.TasFecFin > ahora))
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Playa = playa;
            return View(tarifas);
        }

    }
}
