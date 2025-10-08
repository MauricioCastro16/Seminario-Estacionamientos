using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.Models.ViewModels;
using System.Security.Claims;

namespace estacionamientos.Controllers
{
    public class PlayaEstacionamientoController : Controller
    {
        private readonly AppDbContext _context;

        public PlayaEstacionamientoController(AppDbContext context) => _context = context;

        [HttpGet]
        [Route("Playas")]
        public async Task<IActionResult> Index([FromQuery] PlayasIndexVM vm)
        {
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

            vm.Playas = await query.OrderBy(p => p.PlyNom).ToListAsync();

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

            return View(playa);
        }



        public IActionResult Create() => View(new PlayaEstacionamiento());

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




