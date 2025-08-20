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

        //Retorna la vista principal con los todos los métodos de pago y los métodos de pago aceptados por una playa específica
        [HttpGet("Playas/{plyID}/[controller]")]
        public async Task<IActionResult> Index(int plyId)
        {
            var metodosAceptados = await _ctx.AceptaMetodosPago
                .Where(a => a.PlyID == plyId)
                .AsNoTracking()
                .Select(a => a.MepID)
                .ToListAsync();

            var metodos = await _ctx.MetodosPago.AsNoTracking()
                .OrderBy(m => m.MepNom)
                .ToListAsync();

            var playa = await _ctx.Playas.FirstOrDefaultAsync(p => p.PlyID == plyId);

            if (playa == null)
                return NotFound();

            return View((metodos, metodosAceptados, playa));
        }

        [HttpPost]
        public async Task<IActionResult> Guardar(int plyID, List<int> metodosSeleccionados)
        {
            // Traigo todos los aceptados actuales
            var aceptadosActuales = await _ctx.AceptaMetodosPago
                .Where(a => a.PlyID == plyID)
                .Select(a => a.MepID)
                .ToListAsync();

            // Métodos a agregar (están seleccionados pero no estaban en la BD)
            var nuevos = metodosSeleccionados.Except(aceptadosActuales).ToList();

            foreach (var mepID in nuevos)
            {
                _ctx.AceptaMetodosPago.Add(new AceptaMetodoPago
                {
                    PlyID = plyID,
                    MepID = mepID,
                });
            }

            // Métodos a eliminar (estaban en la BD pero ya no están seleccionados)
            var eliminar = aceptadosActuales.Except(metodosSeleccionados).ToList();

            if (eliminar.Any())
            {
                var registrosAEliminar = await _ctx.AceptaMetodosPago
                    .Where(a => a.PlyID == plyID && eliminar.Contains(a.MepID))
                    .ToListAsync();

                _ctx.AceptaMetodosPago.RemoveRange(registrosAEliminar);
            }

            await _ctx.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { plyId = plyID });
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
            return RedirectToAction(nameof(Index));
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
            // PK fija (si querés poder cambiar método o playa, hacé delete+create)
            if (plyID != model.PlyID || mepID != model.MepID) return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.MepID);
                return View(model);
            }

            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                // Si hay pagos usando (PlyID, MepID) y la FK está en Restrict, puede fallar
                ModelState.AddModelError(string.Empty, "No se puede eliminar: hay pagos que usan este método en esta playa.");
                return View("Delete", item);
            }
        }
    }
}
