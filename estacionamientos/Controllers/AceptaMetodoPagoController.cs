using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class AceptaMetodoPagoController : Controller
    {
        private readonly AppDbContext _ctx;
        public AceptaMetodoPagoController(AppDbContext ctx) => _ctx = ctx;

        private async Task LoadSelects(int? plySel = null, int? mepSel = null)
        {
            var playas = await _ctx.Playas.AsNoTracking()
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyCiu + " - " + p.PlyDir })
                .ToListAsync();

            var metodos = await _ctx.MetodosPago.AsNoTracking()
                .OrderBy(m => m.MepNom)
                .ToListAsync();

            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", plySel);
            ViewBag.MepID = new SelectList(metodos, "MepID", "MepNom", mepSel);
        }

        // Vista principal de métodos de pago para una playa
        [HttpGet("Playas/{plyID}/[controller]")]
        public async Task<IActionResult> Index(int plyID)
        {
            var metodosAceptados = await _ctx.AceptaMetodosPago
                .Where(a => a.PlyID == plyID && a.AmpHab)
                .AsNoTracking()
                .Select(a => a.MepID)
                .ToListAsync();

            var metodos = await _ctx.MetodosPago.AsNoTracking()
                .OrderBy(m => m.MepNom)
                .ToListAsync();

            var playa = await _ctx.Playas.FirstOrDefaultAsync(p => p.PlyID == plyID);

            if (playa == null)
                return NotFound();

            ViewBag.PlyID  = playa.PlyID;
            ViewBag.PlyNom = playa.PlyNom;

            return View((metodos, metodosAceptados, playa));
        }

        // Vista simplificada de Métodos de Pago (solo lectura, para el Playero)
        [HttpGet("Playas/{plyID}/MetodosPago")]
        public async Task<IActionResult> Lista(int plyID)
        {
            var playa = await _ctx.Playas
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlyID == plyID);

            if (playa == null)
                return NotFound();

            var metodosAceptados = await _ctx.AceptaMetodosPago
                .Include(a => a.MetodoPago)
                .AsNoTracking()
                .Where(a => a.PlyID == plyID)
                .ToListAsync();

            ViewBag.Playa = playa;
            return View(metodosAceptados);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(int plyID, List<int> metodosSeleccionados)
        {
            var aceptadosActuales = await _ctx.AceptaMetodosPago
                .Where(a => a.PlyID == plyID && a.AmpHab)
                .Select(a => a.MepID)
                .ToListAsync();

            // Agregar o rehabilitar
            var nuevos = metodosSeleccionados.Except(aceptadosActuales).ToList();
            foreach (var mepID in nuevos)
            {
                var existente = await _ctx.AceptaMetodosPago
                    .FirstOrDefaultAsync(a => a.PlyID == plyID && a.MepID == mepID);

                if (existente != null)
                {
                    // Ya existe pero estaba deshabilitado → lo reactivamos
                    existente.AmpHab = true;
                    _ctx.AceptaMetodosPago.Update(existente);
                }
                else
                {
                    // No existe → lo agregamos
                    _ctx.AceptaMetodosPago.Add(new AceptaMetodoPago { PlyID = plyID, MepID = mepID, AmpHab = true });
                }
            }


            // Deshabilitar en vez de eliminar
                    var eliminar = aceptadosActuales.Except(metodosSeleccionados).ToList();
                    if (eliminar.Any())
                    {
                        var registrosADeshabilitar = await _ctx.AceptaMetodosPago
                            .Where(a => a.PlyID == plyID && eliminar.Contains(a.MepID))
                            .ToListAsync();

                        foreach (var reg in registrosADeshabilitar)
                            reg.AmpHab = false;
                    }

            await _ctx.SaveChangesAsync();

            TempData["SuccessMessage"] = "Los métodos de pago fueron actualizados correctamente.";
            return RedirectToAction("Index", "Playas");
        }

        public async Task<IActionResult> Details(int plyID, int mepID)
        {
            var item = await _ctx.AceptaMetodosPago
                .Include(a => a.Playa)
                .Include(a => a.MetodoPago)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.PlyID == plyID && a.MepID == mepID);

            return item is null ? NotFound() : View(item);
        }

        public async Task<IActionResult> Create()
        {
            await LoadSelects();
            return View(new AceptaMetodoPago { AmpHab = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AceptaMetodoPago model)
        {
            if (await _ctx.AceptaMetodosPago.AnyAsync(a => a.PlyID == model.PlyID && a.MepID == model.MepID))
                ModelState.AddModelError(string.Empty, "La playa ya tiene ese método de pago.");

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.MepID);
                return View(model);
            }

            _ctx.AceptaMetodosPago.Add(model);
            await _ctx.SaveChangesAsync();

            TempData["SuccessMessage"] = "Método de pago agregado correctamente.";
            return RedirectToAction("Index", "Playas");
        }

        public async Task<IActionResult> Edit(int plyID, int mepID)
        {
            var item = await _ctx.AceptaMetodosPago.FindAsync(plyID, mepID);
            if (item is null) return NotFound();

            await LoadSelects(item.PlyID, item.MepID);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int mepID, AceptaMetodoPago model)
        {
            if (plyID != model.PlyID || mepID != model.MepID)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.MepID);
                return View(model);
            }

            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();

            TempData["SuccessMessage"] = "Método de pago actualizado.";
            return RedirectToAction("Index", "Playas");
        }

        public async Task<IActionResult> Delete(int plyID, int mepID)
        {
            var item = await _ctx.AceptaMetodosPago
                .Include(a => a.Playa)
                .Include(a => a.MetodoPago)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.PlyID == plyID && a.MepID == mepID);

            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int mepID)
        {
            var item = await _ctx.AceptaMetodosPago.FindAsync(plyID, mepID);
            if (item is null) return NotFound();

            try
            {
                _ctx.AceptaMetodosPago.Remove(item);
                await _ctx.SaveChangesAsync();

                TempData["SuccessMessage"] = "Método de pago eliminado.";
                return RedirectToAction("Index", "Playas");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "No se puede eliminar: hay pagos que usan este método en esta playa.");
                return View("Delete", item);
            }
        }
    }
}
