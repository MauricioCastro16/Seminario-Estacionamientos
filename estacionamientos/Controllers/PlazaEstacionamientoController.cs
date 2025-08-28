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

            ViewBag.PlyID  = playa.PlyID;
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

            if (cantidad < 1 || plzTecho == null || (plzTecho == true && plzAlt == null))
            {
               ViewBag.PlyID = playa.PlyID;
               ViewBag.PlyNom = playa.PlyNom;
               ViewBag.DefaultCantidad = 1;

              var plazas = playa.Plazas.OrderBy(z => z.PlzNum).ToList();

            if (plzTecho == true && plzAlt == null)
                ModelState.AddModelError("plzAlt", "Si selecciona Techo = Sí, debe ingresar una altura.");
            else
                ModelState.AddModelError(string.Empty, "Todos los campos son obligatorios y la cantidad debe ser mayor a 0.");

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
                    PlzAlt = plzAlt,
                    PlzHab = true
                };
                _ctx.Plazas.Add(plaza);
            }

            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(ConfigurarPlazas), new { plyID =plyID });
        }


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PlazaEstacionamiento model)
        {
            if (await _ctx.Plazas.AnyAsync(p => p.PlyID == model.PlyID && p.PlzNum == model.PlzNum))
                ModelState.AddModelError(nameof(model.PlzNum), "Ya existe esa plaza en la playa seleccionada.");

            if (!ModelState.IsValid)
            {
                await LoadPlayas(model.PlyID);
                return View(model);
            }

            _ctx.Plazas.Add(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int plyID, int plzNum)
        {
            var item = await _ctx.Plazas.FindAsync(plyID, plzNum);
            if (item is null) return NotFound();

            await LoadPlayas(item.PlyID);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int plzNum, PlazaEstacionamiento model)
        {
            if (plyID != model.PlyID || plzNum != model.PlzNum) return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadPlayas(model.PlyID);
                return View(model);
            }

            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int plyID, int plzNum)
        {
            var item = await _ctx.Plazas.Include(p => p.Playa).AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlyID == plyID && p.PlzNum == plzNum);
            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int plzNum)
        {
            var item = await _ctx.Plazas.FindAsync(plyID, plzNum);
            if (item is null) return NotFound();

            _ctx.Plazas.Remove(item);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
