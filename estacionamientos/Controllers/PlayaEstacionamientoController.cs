using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.Models.ViewModels;
using System.Security.Claims;

namespace estacionamientos.Controllers
{
    public class PlayaEstacionamientoController : BaseController
    {
        private readonly AppDbContext _context;

        public PlayaEstacionamientoController(AppDbContext context) => _context = context;

        [HttpGet]
        [Route("Playas")]
        public async Task<IActionResult> Index([FromQuery] PlayasIndexVM vm)
        {

            SetBreadcrumb(
                new BreadcrumbItem { Title = "Playas", Url = Url.Action("Index", "PlayaEstacionamiento")! }
            );
            // 1) Usuario actual (seguro ante parseo)
            int usuNU = 0;
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuNU);

            // 2) Query base: SOLO las playas administradas por el usuario
            var baseQuery = _context.Playas
                .AsNoTracking()
                .Where(p => _context.AdministraPlayas
                    .Any(ap => ap.PlyID == p.PlyID && ap.DueNU == usuNU));

            vm.ProvinciasCombo = await baseQuery
                .Where(p => !string.IsNullOrEmpty(p.PlyProv))
                .Select(p => p.PlyProv!)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            var allowed = new[] { "all", "nombre", "provincia", "ciudad", "direccion" };
            vm.FilterBy = (vm.FilterBy ?? "all").ToLower();
            if (!allowed.Contains(vm.FilterBy)) vm.FilterBy = "all";

            static void Normalize(List<string> list)
            {
                if (list == null) return;
                var flat = new List<string>();
                foreach (var item in list)
                {
                    if (string.IsNullOrWhiteSpace(item)) continue;
                    var parts = item.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var p in parts)
                        if (!string.IsNullOrWhiteSpace(p)) flat.Add(p.Trim());
                }
                list.Clear();
                list.AddRange(flat.Distinct(StringComparer.OrdinalIgnoreCase));
            }
            Normalize(vm.Nombres);
            Normalize(vm.Provincias);
            Normalize(vm.Ciudades);
            Normalize(vm.Direcciones);
            Normalize(vm.Todos);

            if (!string.IsNullOrWhiteSpace(vm.Remove) && vm.Remove.Contains(':'))
            {
                var parts = vm.Remove.Split(':', 2);
                var key = parts[0].ToLower().Trim();
                var val = parts[1].Trim();

                void RemoveFrom(List<string> list)
                {
                    list.RemoveAll(x => string.Equals(x?.Trim(), val, System.StringComparison.OrdinalIgnoreCase));
                }

                switch (key)
                {
                    case "nombre": RemoveFrom(vm.Nombres); break;
                    case "provincia": RemoveFrom(vm.Provincias); break;
                    case "ciudad": RemoveFrom(vm.Ciudades); break;
                    case "direccion": RemoveFrom(vm.Direcciones); break;
                    case "todos": RemoveFrom(vm.Todos); break;

                }

                vm.Remove = null;
            }

            var query = baseQuery.AsQueryable();

            if (!string.IsNullOrWhiteSpace(vm.Q))
            {
                var q = $"%{vm.Q.Trim()}%";
                switch (vm.FilterBy)
                {
                    case "nombre":
                        query = query.Where(p => EF.Functions.ILike(p.PlyNom!, q));
                        break;
                    case "ciudad":
                        query = query.Where(p => EF.Functions.ILike(p.PlyCiu!, q));
                        break;
                    case "direccion":
                        query = query.Where(p => EF.Functions.ILike(p.PlyDir!, q));
                        break;
                    case "all":
                    case "provincia":
                    default:
                        query = query.Where(p =>
                            EF.Functions.ILike(p.PlyNom!, q) ||
                            EF.Functions.ILike(p.PlyProv!, q) ||
                            EF.Functions.ILike(p.PlyCiu!, q) ||
                            EF.Functions.ILike(p.PlyDir!, q));
                        break;
                }
            }

            if (vm.Nombres != null && vm.Nombres.Count > 0)
            {
                var patrones = vm.Nombres
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => "%" + s.Trim() + "%")
                    .ToArray();

                query = query.Where(p => patrones.Any(pat =>
                    EF.Functions.ILike(p.PlyNom!, pat)));
            }

            if ((vm.Provincias?.Count ?? 0) > 0 || !string.IsNullOrWhiteSpace(vm.SelectedOption))
            {
                var provs = new List<string>();

                if (vm.Provincias != null)
                    provs.AddRange(vm.Provincias
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim()));

                if (!string.IsNullOrWhiteSpace(vm.SelectedOption))
                    provs.Add(vm.SelectedOption.Trim());

                var provsLower = provs.Select(pr => pr.ToLower()).ToList();
                query = query.Where(p => provsLower.Contains(p.PlyProv!.ToLower()));

            }

            if (vm.Ciudades != null && vm.Ciudades.Count > 0)
            {
                var patrones = vm.Ciudades
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => "%" + s.Trim() + "%")
                    .ToArray();

                query = query.Where(p => patrones.Any(pat =>
                    EF.Functions.ILike(p.PlyCiu!, pat)));
            }

            if (vm.Direcciones != null && vm.Direcciones.Count > 0)
            {
                var patrones = vm.Direcciones
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => "%" + s.Trim() + "%")
                    .ToArray();

                query = query.Where(p => patrones.Any(pat =>
                    EF.Functions.ILike(p.PlyDir!, pat)));
            }

            if (vm.Todos != null && vm.Todos.Count > 0)
            {
                var patrones = vm.Todos
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => "%" + s.Trim() + "%")
                    .ToArray();

                query = query.Where(p => patrones.Any(pat =>
                    EF.Functions.ILike(p.PlyNom!, pat) ||
                    EF.Functions.ILike(p.PlyProv!, pat) ||
                    EF.Functions.ILike(p.PlyCiu!, pat) ||
                    EF.Functions.ILike(p.PlyDir!, pat)));
            }

            var playas = await query.OrderBy(p => p.PlyNom).ToListAsync();

            // Para cada playa en borrador, verificar qué falta
            foreach (var playa in playas.Where(p => p.PlyEstado == EstadoPlaya.Borrador))
            {
                var tieneMetodoPago = await _context.AceptaMetodosPago
                    .AnyAsync(a => a.PlyID == playa.PlyID && a.AmpHab);
                var tienePlaza = await _context.Plazas
                    .AnyAsync(p => p.PlyID == playa.PlyID);

                // Guardar información en ViewBag para usar en la vista
                ViewData[$"FaltaMetodoPago_{playa.PlyID}"] = !tieneMetodoPago;
                ViewData[$"FaltaPlaza_{playa.PlyID}"] = !tienePlaza;
            }

            vm.Playas = playas;

            if (!vm.HayFiltros && Request.QueryString.HasValue)
                return RedirectToAction(nameof(Index));

            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var playa = await _context.Playas
                .Include(p => p.Valoraciones)
                .Include(p => p.Horarios)
                    .ThenInclude(h => h.ClasificacionDias)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlyID == id);

            if (playa is null) return NotFound();

            ViewBag.Clasificaciones = await _context.ClasificacionesDias
                .AsNoTracking()
                .OrderBy(c => c.ClaDiasID)
                .ToListAsync();

            // Turno abierto mas reciente en esta playa (si hay)
            var turnoAbierto = await _context.Turnos
                .Include(t => t.Playero)
                .AsNoTracking()
                .Where(t => t.PlyID == id && t.TurFyhFin == null)
                .OrderByDescending(t => t.TurFyhIni)
                .FirstOrDefaultAsync();

            ViewBag.TurnoAbierto = turnoAbierto; // lo usamos en la vista
            return View(playa);
        }

        public async Task<IActionResult> DetailsPlayero(int id)
        {
            var playa = await _context.Playas
                .Include(p => p.Horarios)
                    .ThenInclude(h => h.ClasificacionDias)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlyID == id);

            if (playa is null) return NotFound();

            ViewBag.Clasificaciones = await _context.ClasificacionesDias
                .AsNoTracking()
                .OrderBy(c => c.ClaDiasID)
                .ToListAsync();

            // Métodos de pago aceptados
            ViewBag.MetodosPago = await _context.AceptaMetodosPago
                .Include(a => a.MetodoPago)
                .Where(a => a.PlyID == id && a.AmpHab)
                .AsNoTracking()
                .ToListAsync();

            // Plazas de la playa
            ViewBag.Plazas = await _context.Plazas
                .Include(p => p.Clasificaciones)
                    .ThenInclude(pc => pc.Clasificacion)
                .Where(p => p.PlyID == id)
                .OrderBy(p => p.PlzNum)
                .Select(p => new
                {
                    p.PlyID,
                    p.PlzNum,
                    p.PlzNombre,
                    p.PlzTecho,
                    p.PlzAlt,
                    p.PlzHab,
                    PlzOcupada = _context.Ocupaciones.Any(o => o.PlyID == p.PlyID && o.PlzNum == p.PlzNum && o.OcufFyhFin == null),
                    Clasificaciones = p.Clasificaciones.Select(pc => pc.Clasificacion.ClasVehTipo).ToList()
                })
                .AsNoTracking()
                .ToListAsync();

            // Tarifas vigentes
            var ahora = DateTime.UtcNow;
            ViewBag.Tarifas = await _context.TarifasServicio
                .Include(t => t.ServicioProveido)
                    .ThenInclude(sp => sp.Servicio)
                .Include(t => t.ClasificacionVehiculo)
                .Where(t => t.PlyID == id 
                    && t.TasFecIni <= ahora 
                    && (t.TasFecFin == null || t.TasFecFin >= ahora)
                    && t.ServicioProveido.SerProvHab)
                .AsNoTracking()
                .ToListAsync();

            SetBreadcrumb(
                new BreadcrumbItem { Title = playa.PlyNom, Url = Url.Action("DetailsPlayero", "PlayaEstacionamiento", new { id = playa.PlyID})! }
            );

            return View(playa);
        }



        public IActionResult Create()
        {
            SetBreadcrumb(
                new BreadcrumbItem { Title = "Playas", Url = Url.Action("Index", "PlayaEstacionamiento")! },
                new BreadcrumbItem { Title = "Agregar Playa", Url = Url.Action("Create", "PlayaEstacionamiento")! }
            );
            return View(new PlayaEstacionamiento());
        }    

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PlayaEstacionamiento model)
        {
            if (!ModelState.IsValid) return View(model);

            // Calcular el siguiente PlyID disponible dinámicamente
            int nextPlyId = Math.Max(1, (await _context.Playas.MaxAsync(p => p.PlyID)) + 1);

            // Verificar que no haya colisión con el valor de PlyID
            while (await _context.Playas.AnyAsync(p => p.PlyID == nextPlyId))
            {
                nextPlyId++;  // Incrementar hasta encontrar un PlyID disponible
            }

            // Asignar el PlyID calculado al modelo de la playa
            model.PlyID = nextPlyId;
            
            // Guardar como Borrador por defecto
            model.PlyEstado = EstadoPlaya.Borrador;

            // Agregar la nueva playa a la base de datos
            _context.Playas.Add(model);
            await _context.SaveChangesAsync();

            // Asociar la playa creada con el dueño actual
            var nameIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(nameIdentifier))
                return Unauthorized();

            var usuNU = int.Parse(nameIdentifier);

            _context.AdministraPlayas.Add(new AdministraPlaya
            {
                PlyID = model.PlyID,
                DueNU = usuNU
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Verifica si una playa tiene al menos 1 método de pago habilitado y 1 plaza,
        /// y actualiza el estado a Vigente si cumple los requisitos.
        /// </summary>
        public async Task VerificarYActualizarEstado(int plyID)
        {
            var playa = await _context.Playas.FindAsync(plyID);
            if (playa == null) return;

            // Verificar si tiene al menos 1 método de pago habilitado
            var tieneMetodoPago = await _context.AceptaMetodosPago
                .AnyAsync(a => a.PlyID == plyID && a.AmpHab);

            // Verificar si tiene al menos 1 plaza
            var tienePlaza = await _context.Plazas
                .AnyAsync(p => p.PlyID == plyID);

            // Si cumple ambos requisitos, actualizar a Vigente
            if (tieneMetodoPago && tienePlaza && playa.PlyEstado == EstadoPlaya.Borrador)
            {
                playa.PlyEstado = EstadoPlaya.Vigente;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Cambia el estado de una playa (Borrador <-> Vigente).
        /// Solo permite cambiar a Vigente si cumple los requisitos.
        /// Solo permite cambiar a Borrador si no tiene ocupaciones activas, turnos abiertos o abonos vigentes.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Duenio")]
        public async Task<IActionResult> ToggleEstado(int plyID, bool nuevoEstado)
        {
            // Verificar que el usuario sea dueño de la playa
            int usuNU = 0;
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuNU);

            var esMia = await _context.AdministraPlayas
                .AnyAsync(a => a.PlyID == plyID && a.DueNU == usuNU);
            
            if (!esMia)
                return Forbid();

            var playa = await _context.Playas.FindAsync(plyID);
            if (playa == null)
                return NotFound();

            // Si se intenta cambiar a Vigente, verificar requisitos
            if (nuevoEstado && playa.PlyEstado == EstadoPlaya.Borrador)
            {
                var tieneMetodoPago = await _context.AceptaMetodosPago
                    .AnyAsync(a => a.PlyID == plyID && a.AmpHab);
                
                var tienePlaza = await _context.Plazas
                    .AnyAsync(p => p.PlyID == plyID);

                if (!tieneMetodoPago || !tienePlaza)
                {
                    TempData["ErrorMessage"] = "No se puede cambiar a Vigente. La playa debe tener al menos 1 método de pago habilitado y 1 plaza configurada.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Si se intenta cambiar a Borrador, verificar que no tenga relaciones activas
            if (!nuevoEstado && playa.PlyEstado == EstadoPlaya.Vigente)
            {
                var ahora = DateTime.UtcNow;
                var razones = new List<string>();

                // Verificar ocupaciones activas
                var tieneOcupacionesActivas = await _context.Ocupaciones
                    .AnyAsync(o => o.PlyID == plyID && o.OcufFyhFin == null);
                if (tieneOcupacionesActivas)
                    razones.Add("ocupaciones activas");

                // Verificar turnos abiertos
                var tieneTurnosAbiertos = await _context.Turnos
                    .AnyAsync(t => t.PlyID == plyID && t.TurFyhFin == null);
                if (tieneTurnosAbiertos)
                    razones.Add("turnos de playeros abiertos");

                // Verificar abonos activos (abonos con estado Activo o que no hayan finalizado)
                var tieneAbonosActivos = await _context.Abonos
                    .AnyAsync(a => a.PlyID == plyID && 
                        (a.EstadoPago == EstadoPago.Activo || 
                         (a.AboFyhFin == null || a.AboFyhFin > ahora)));
                if (tieneAbonosActivos)
                    razones.Add("abonos vigentes");

                if (razones.Count > 0)
                {
                    var mensaje = "No se puede cambiar a Borrador. La playa tiene: " + 
                        string.Join(", ", razones) + ".";
                    TempData["ErrorMessage"] = mensaje;
                    return RedirectToAction(nameof(Index));
                }
            }

            // Cambiar el estado
            playa.PlyEstado = nuevoEstado ? EstadoPlaya.Vigente : EstadoPlaya.Borrador;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Estado de la playa actualizado a {(nuevoEstado ? "Vigente" : "Borrador")}.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var playa = await _context.Playas.FindAsync(id);
            return playa is null ? NotFound() : View(playa);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PlayaEstacionamiento model)
        {
            if (id != model.PlyID) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var playa = await _context.Playas.AsNoTracking().FirstOrDefaultAsync(p => p.PlyID == id);
            return playa is null ? NotFound() : View(playa);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var playa = await _context.Playas.FindAsync(id);
            if (playa is null) return NotFound();
            _context.Playas.Remove(playa);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


    }
}




