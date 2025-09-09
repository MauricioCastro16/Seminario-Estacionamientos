using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class PlazaEstacionamientoController : Controller
    {
        private readonly AppDbContext _ctx;
        public PlazaEstacionamientoController(AppDbContext ctx) => _ctx = ctx;

        private async Task LoadPlayas(int? selected = null)
        {
            var playas = await _ctx.Playas.AsNoTracking()
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyCiu + " - " + p.PlyDir })
                .ToListAsync();
            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", selected);
        }

        public async Task<IActionResult> Index()
        {
            var q = _ctx.Plazas.Include(p => p.Playa).AsNoTracking();
            return View(await q.ToListAsync());
        }

        public async Task<IActionResult> Details(int plyID, int plzNum)
        {
            var item = await _ctx.Plazas.Include(p => p.Playa).AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlyID == plyID && p.PlzNum == plzNum);
            return item is null ? NotFound() : View(item);
        }

        [HttpGet("Playas/{plyID}/[controller]")]
        public async Task<IActionResult> ConfigurarPlazas(int plyID)
        {
            var playa = await _ctx.Playas
             .Include(p => p.Plazas)
             .AsNoTracking()
             .FirstOrDefaultAsync(p => p.PlyID == plyID);

            if (playa == null) return NotFound();

            ViewBag.PlyID = playa.PlyID;
            ViewBag.PlyNom = playa.PlyNom;
            ViewBag.DefaultCantidad = 1;

            return View(playa.Plazas.OrderBy(z => z.PlzNum));
        }

        [HttpPost("Playas/{plyID}/[controller]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfigurarPlazas(
            int plyID,
            int cantidad = 1,
            bool? plzTecho = null,
            decimal? plzAlt = null)
        {
            var playa = await _ctx.Playas
                .Include(p => p.Plazas)
                .FirstOrDefaultAsync(p => p.PlyID == plyID);

            if (playa == null) return NotFound();

            //si no hay techo, no hay altura
            if (plzTecho == false)
                plzAlt = null;

            // Validar altura según techo
            bool alturaValida =
                (plzTecho == true && plzAlt.HasValue && plzAlt.Value >= 2m) ||
                (plzTecho == false && plzAlt == null);

            if (cantidad < 1 || plzTecho == null || !alturaValida)
            {
                ViewBag.PlyID = playa.PlyID;
                ViewBag.PlyNom = playa.PlyNom;
                ViewBag.DefaultCantidad = 1;

                var plazas = playa.Plazas.OrderBy(z => z.PlzNum).ToList();

                if (plzTecho == true && (!plzAlt.HasValue || plzAlt.Value < 2m))
                {
                    ModelState.AddModelError("plzAlt",
                        "La altura mínima permitida es 2m.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty,
                        "Todos los campos son obligatorios.");
                }

                return View(plazas);
            }

            // calcular desde qué número crear
            int nextNum = playa.Plazas.Any() ? playa.Plazas.Max(pl => pl.PlzNum) + 1 : 1;

            for (int i = 0; i < cantidad; i++)
            {
                var plaza = new PlazaEstacionamiento
                {
                    PlyID = plyID,
                    PlzNum = nextNum + i,
                    PlzTecho = plzTecho.Value,
                    PlzAlt = plzTecho.Value ? plzAlt : null,
                    PlzHab = true
                };
                _ctx.Plazas.Add(plaza);
            }

            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(ConfigurarPlazas), new { plyID = plyID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInline(int plyID, int plzNum, bool plzTecho, decimal? plzAlt)
        {
            var plaza = await _ctx.Plazas.FindAsync(plyID, plzNum);
            if (plaza is null)
            {
                TempData["Error"] = $"No se encontró la plaza {plzNum}.";
                return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
            }

            //sin techo => sin altura
            if (!plzTecho) plzAlt = null;

            // con techo => altura >= 2
            if (plzTecho && (!plzAlt.HasValue || plzAlt.Value < 2m))
            {
                TempData["Error"] = "La altura mínima permitida es 2m";
                return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
            }

            plaza.PlzTecho = plzTecho;
            plaza.PlzAlt = plzAlt;

            await _ctx.SaveChangesAsync();
            TempData["Ok"] = $"Plaza {plzNum} actualizada.";
            return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInline(int plyID, int plzNum)
        {
            var plaza = await _ctx.Plazas.FindAsync(plyID, plzNum);
            if (plaza is null)
            {
                TempData["Error"] = $"No se encontró la plaza {plzNum}.";
                return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
            }

            _ctx.Plazas.Remove(plaza);
            await _ctx.SaveChangesAsync();

            TempData["Ok"] = $"Plaza {plzNum} eliminada.";
            return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleHabilitadaDueño(int plyID, int plzNum)
        {
            var plaza = await _ctx.Plazas.FindAsync(plyID, plzNum);
            if (plaza is null)
            {
                TempData["Error"] = $"No se encontró la plaza {plzNum}.";
                TempData["MensajeCss"] = "danger";
                return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
            }

            plaza.PlzHab = !plaza.PlzHab;
            await _ctx.SaveChangesAsync();

            TempData["Ok"] = $"Plaza {plzNum} {(plaza.PlzHab ? "habilitada" : "deshabilitada")}.";
            TempData["MensajeCss"] = plaza.PlzHab ? "success" : "danger";

            return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
        }


    }
}