using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.ViewModels;

namespace estacionamientos.Controllers
{
    public class ServicioProveidoController : Controller
    {
        private readonly AppDbContext _ctx;
        public ServicioProveidoController(AppDbContext ctx) => _ctx = ctx;

        private async Task LoadSelects(int? plySel = null, int? serSel = null)
        {
            var playas = await _ctx.Playas.AsNoTracking()
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyCiu + " - " + p.PlyDir })
                .ToListAsync();
            var servicios = await _ctx.Servicios.AsNoTracking()
                .OrderBy(s => s.SerNom).ToListAsync();

            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", plySel);
            ViewBag.SerID = new SelectList(servicios, "SerID", "SerNom", serSel);
        }

        public async Task<IActionResult> Index()
        {
            var q = _ctx.ServiciosProveidos
                .Include(sp => sp.Playa)
                .Include(sp => sp.Servicio)
                .AsNoTracking();
            return View(await q.ToListAsync());
        }

        public async Task<IActionResult> Details(int plyID, int serID)
        {
            var item = await _ctx.ServiciosProveidos
                .Include(sp => sp.Playa)
                .Include(sp => sp.Servicio)
                .AsNoTracking()
                .FirstOrDefaultAsync(sp => sp.PlyID == plyID && sp.SerID == serID);
            return item is null ? NotFound() : View(item);
        }

        public async Task<IActionResult> Create()
        {
            await LoadSelects();
            return View(new ServicioProveido());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServicioProveido model)
        {
            if (await _ctx.ServiciosProveidos.AnyAsync(sp => sp.PlyID == model.PlyID && sp.SerID == model.SerID))
                ModelState.AddModelError("", "La playa ya tiene ese servicio.");

            if (!ModelState.IsValid)
            {
                _ctx.ServiciosProveidos.Add(model);
                await LoadSelects(model.PlyID, model.SerID);
                return View(model);
            }

            _ctx.ServiciosProveidos.Add(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int plyID, int serID)
        {
            var item = await _ctx.ServiciosProveidos.FindAsync(plyID, serID);
            if (item is null) return NotFound();
            await LoadSelects(item.PlyID, item.SerID);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int serID, ServicioProveido model)
        {
            if (plyID != model.PlyID || serID != model.SerID) return BadRequest();
            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.SerID);
                return View(model);
            }
            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int plyID, int serID)
        {
            var item = await _ctx.ServiciosProveidos
                .Include(sp => sp.Playa)
                .Include(sp => sp.Servicio)
                .AsNoTracking()
                .FirstOrDefaultAsync(sp => sp.PlyID == plyID && sp.SerID == serID);
            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int serID)
        {
            var item = await _ctx.ServiciosProveidos.FindAsync(plyID, serID);
            if (item is null) return NotFound();
            _ctx.ServiciosProveidos.Remove(item);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Servicios(int plyID)
        {
            var playa = await _ctx.Playas
                .FirstOrDefaultAsync(p => p.PlyID == plyID);

            if (playa == null)
            {
                return NotFound(); // Si la playa no existe, devolver error
            }

            // Obtener todos los servicios disponibles
            var serviciosDisponibles = await _ctx.Servicios.AsNoTracking().ToListAsync();

            // Obtener los servicios ya asociados a la playa
            var serviciosAsignados = await _ctx.ServiciosProveidos
                .Where(sp => sp.PlyID == plyID)
                .Select(sp => sp.SerID)
                .ToListAsync();

            // Pasar los servicios a la vista
            var viewModel = new ServiciosViewModel
            {
                PlayaID = plyID,
                PlayaNom = playa.PlyNom,
                ServiciosDisponibles = serviciosDisponibles,
                ServiciosAsignados = serviciosAsignados
            };

            return View(viewModel);  // Pasar los datos al modelo de vista
        }

        public async Task<IActionResult> CambiarEstado(int plyID, int serID, bool habilitado)
        {
            var servicioProveido = await _ctx.ServiciosProveidos
                .FirstOrDefaultAsync(sp => sp.PlyID == plyID && sp.SerID == serID);
            
            if (servicioProveido != null)
            {
                servicioProveido.SerProvHab = habilitado;
                await _ctx.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Servicios), new { plyID });
        }

        [HttpPost]
        public async Task<IActionResult> AsignarServicios(ServiciosViewModel model)
        {
            // Eliminar los servicios existentes asociados a la playa
            var serviciosExistentes = await _ctx.ServiciosProveidos
                .Where(sp => sp.PlyID == model.PlayaID)
                .ToListAsync();

            _ctx.ServiciosProveidos.RemoveRange(serviciosExistentes);

            foreach (var servicioID in model.ServiciosAsignados)
            {
                _ctx.ServiciosProveidos.Add(new ServicioProveido
                {
                    PlyID = model.PlayaID,
                    SerID = servicioID, // Ahora estamos agregando el ID del servicio
                    SerProvHab = true // Estado activo
                });
            }


            await _ctx.SaveChangesAsync();

            return RedirectToAction("Index", "PlayaEstacionamiento"); // O la página que prefieras
        }


    }

}
