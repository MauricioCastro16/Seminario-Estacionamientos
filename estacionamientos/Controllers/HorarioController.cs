using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.Models.ViewModels;

namespace estacionamientos.Controllers
{
    [Authorize(Roles = "Duenio")]
    public class HorarioController : Controller
    {
        private readonly AppDbContext _ctx;
        private static readonly DateTime BaseDate = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public HorarioController(AppDbContext ctx) => _ctx = ctx;

        private int? CurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : null;
        }

        private static DateTime BuildDate(TimeSpan time) => DateTime.SpecifyKind(BaseDate.Add(time), DateTimeKind.Utc);
        private static TimeSpan ReadTime(DateTime date) => date.TimeOfDay;

        private async Task<bool> OwnsPlayaAsync(int dueNu, int plyID)
            => await _ctx.AdministraPlayas.AnyAsync(ap => ap.DueNU == dueNu && ap.PlyID == plyID);

        private async Task<PlayaEstacionamiento?> GetPlayaAsync(int plyID)
            => await _ctx.Playas.AsNoTracking().FirstOrDefaultAsync(p => p.PlyID == plyID);

        private async Task<List<PlayaEstacionamiento>> GetPlayasDelDuenioAsync(int dueNu)
            => await _ctx.Playas
                .AsNoTracking()
                .Where(p => _ctx.AdministraPlayas.Any(ap => ap.DueNU == dueNu && ap.PlyID == p.PlyID))
                .OrderBy(p => p.PlyCiu)
                .ThenBy(p => p.PlyDir)
                .ToListAsync();

        private async Task<List<SelectListItem>> BuildClasificacionesAsync(int plyID, int? selected = null)
        {
            var clasifs = await _ctx.ClasificacionesDias
                .AsNoTracking()
                .OrderBy(c => c.ClaDiasID)
                .ToListAsync();

            return clasifs.Select(c => new SelectListItem
            {
                Value = c.ClaDiasID.ToString(),
                Text = string.IsNullOrWhiteSpace(c.ClaDiasDesc)
                    ? c.ClaDiasTipo
                    : $"{c.ClaDiasTipo} - {c.ClaDiasDesc}",
                Selected = selected == c.ClaDiasID
            }).ToList();
        }

        private static HorariosIndexVM MapToVm(PlayaEstacionamiento playa, IEnumerable<ClasificacionDias> clasifs, IEnumerable<Horario> horarios)
        {
            var vm = new HorariosIndexVM
            {
                PlyID = playa.PlyID,
                PlayaNombre = playa.PlyNom,
                PlayaCiudad = playa.PlyCiu,
                PlayaDireccion = playa.PlyDir
            };

            foreach (var cla in clasifs)
            {
                var slots = horarios
                    .Where(h => h.ClaDiasID == cla.ClaDiasID)
                    .OrderBy(h => h.HorFyhIni)
                    .Select(h => new HorarioSlotVM
                    {
                        Inicio = h.HorFyhIni,
                        Fin = h.HorFyhFin
                    })
                    .ToList();

                vm.Clasificaciones.Add(new HorarioGroupVM
                {
                    ClasificacionId = cla.ClaDiasID,
                    ClasificacionNombre = cla.ClaDiasTipo,
                    ClasificacionDescripcion = cla.ClaDiasDesc,
                    Franjas = slots
                });
            }

            return vm;
        }

        public async Task<IActionResult> Index(int? plyID = null)
        {
            var dueNu = CurrentUserId();
            if (!dueNu.HasValue) return Unauthorized();

            var playas = await GetPlayasDelDuenioAsync(dueNu.Value);
            if (playas.Count == 0)
            {
                ViewBag.Playas = new SelectList(Enumerable.Empty<SelectListItem>());
                ViewBag.NoPlayas = true;
                return View(new HorariosIndexVM());
            }

            var selectedId = plyID.HasValue && playas.Any(p => p.PlyID == plyID.Value)
                ? plyID.Value
                : playas.First().PlyID;

            var playaSeleccionada = playas.First(p => p.PlyID == selectedId);

            var clasifs = await _ctx.ClasificacionesDias
                .AsNoTracking()
                .OrderBy(c => c.ClaDiasID)
                .ToListAsync();

            var horarios = await _ctx.Horarios
                .AsNoTracking()
                .Where(h => h.PlyID == selectedId)
                .ToListAsync();

            var vm = MapToVm(playaSeleccionada, clasifs, horarios);

            ViewBag.Playas = new SelectList(
                playas.Select(p => new { p.PlyID, Nombre = $"{p.PlyCiu} - {p.PlyDir}" }),
                "PlyID",
                "Nombre",
                selectedId);

            return View(vm);
        }

        public async Task<IActionResult> Create(int plyID, int? claDiasID = null)
        {
            var dueNu = CurrentUserId();
            if (!dueNu.HasValue) return Unauthorized();

            if (!await OwnsPlayaAsync(dueNu.Value, plyID))
                return NotFound();

            var playa = await GetPlayaAsync(plyID);
            if (playa is null) return NotFound();

            var vm = new HorarioFormVM
            {
                PlyID = playa.PlyID,
                PlayaNombre = playa.PlyNom,
                PlayaResumen = $"{playa.PlyCiu} - {playa.PlyDir}",
                ClaDiasID = claDiasID ?? 0,
                HoraApertura = new TimeSpan(8, 0, 0),
                HoraCierre = new TimeSpan(20, 0, 0),
                Clasificaciones = await BuildClasificacionesAsync(plyID, claDiasID)
            };

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HorarioFormVM model)
        {
            var dueNu = CurrentUserId();
            if (!dueNu.HasValue) return Unauthorized();

            if (!await OwnsPlayaAsync(dueNu.Value, model.PlyID))
                return NotFound();

            if (model.HoraCierre <= model.HoraApertura)
                ModelState.AddModelError(nameof(model.HoraCierre), "La hora de cierre debe ser posterior a la hora de apertura.");

            var nuevaApertura = BuildDate(model.HoraApertura);
            var nuevoCierre = BuildDate(model.HoraCierre);

            if (await _ctx.Horarios.AnyAsync(h =>
                h.PlyID == model.PlyID &&
                h.ClaDiasID == model.ClaDiasID &&
                h.HorFyhIni == nuevaApertura))
            {
                ModelState.AddModelError(string.Empty, "Ya existe un horario con esa clasificacion y horario de apertura.");
            }

            var playa = await GetPlayaAsync(model.PlyID);
            if (playa is null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.Clasificaciones = await BuildClasificacionesAsync(model.PlyID, model.ClaDiasID);
                model.PlayaNombre = playa.PlyNom;
                model.PlayaResumen = $"{playa.PlyCiu} - {playa.PlyDir}";
                return View(model);
            }

            var horario = new Horario
            {
                PlyID = model.PlyID,
                ClaDiasID = model.ClaDiasID,
                HorFyhIni = nuevaApertura,
                HorFyhFin = nuevoCierre
            };

            _ctx.Horarios.Add(horario);
            await _ctx.SaveChangesAsync();

            TempData["HorarioMessage"] = "Horario cargado correctamente.";
            return RedirectToAction(nameof(Index), new { plyID = model.PlyID });
        }

        public async Task<IActionResult> Edit(int plyID, int claDiasID, DateTime horFyhIni)
        {
            var dueNu = CurrentUserId();
            if (!dueNu.HasValue) return Unauthorized();

            if (!await OwnsPlayaAsync(dueNu.Value, plyID))
                return NotFound();

            var horario = await _ctx.Horarios
                .Include(h => h.Playa)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.PlyID == plyID && h.ClaDiasID == claDiasID && h.HorFyhIni == horFyhIni);

            if (horario is null) return NotFound();

            var vm = new HorarioFormVM
            {
                PlyID = horario.PlyID,
                PlayaNombre = horario.Playa.PlyNom,
                PlayaResumen = $"{horario.Playa.PlyCiu} - {horario.Playa.PlyDir}",
                ClaDiasID = horario.ClaDiasID,
                HoraApertura = ReadTime(horario.HorFyhIni),
                HoraCierre = ReadTime(horario.HorFyhFin ?? horario.HorFyhIni.AddHours(1)),
                Clasificaciones = await BuildClasificacionesAsync(plyID, horario.ClaDiasID),
                HorFyhIniOriginal = horario.HorFyhIni,
                ClaDiasIDOriginal = horario.ClaDiasID,
                EsEdicion = true
            };

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int claDiasID, DateTime horFyhIni, HorarioFormVM model)
        {
            var dueNu = CurrentUserId();
            if (!dueNu.HasValue) return Unauthorized();

            if (plyID != model.PlyID) return BadRequest();

            if (!await OwnsPlayaAsync(dueNu.Value, model.PlyID))
                return NotFound();

            if (model.HoraCierre <= model.HoraApertura)
                ModelState.AddModelError(nameof(model.HoraCierre), "La hora de cierre debe ser posterior a la hora de apertura.");

            var existente = await _ctx.Horarios
                .FirstOrDefaultAsync(h => h.PlyID == plyID && h.ClaDiasID == claDiasID && h.HorFyhIni == horFyhIni);

            if (existente is null) return NotFound();

            var nuevaApertura = BuildDate(model.HoraApertura);
            var nuevoCierre = BuildDate(model.HoraCierre);

            var hayDuplicado = await _ctx.Horarios.AnyAsync(h =>
                h.PlyID == model.PlyID &&
                h.ClaDiasID == model.ClaDiasID &&
                h.HorFyhIni == nuevaApertura &&
                !(h.PlyID == existente.PlyID && h.ClaDiasID == existente.ClaDiasID && h.HorFyhIni == existente.HorFyhIni));

            if (hayDuplicado)
                ModelState.AddModelError(string.Empty, "Ya existe un horario con esa clasificacion y horario de apertura.");

            var playa = await GetPlayaAsync(model.PlyID);
            if (playa is null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.Clasificaciones = await BuildClasificacionesAsync(model.PlyID, model.ClaDiasID);
                model.PlayaNombre = playa.PlyNom;
                model.PlayaResumen = $"{playa.PlyCiu} - {playa.PlyDir}";
                model.EsEdicion = true;
                return View(model);
            }

            _ctx.Horarios.Remove(existente);
            _ctx.Horarios.Add(new Horario
            {
                PlyID = model.PlyID,
                ClaDiasID = model.ClaDiasID,
                HorFyhIni = nuevaApertura,
                HorFyhFin = nuevoCierre
            });

            await _ctx.SaveChangesAsync();

            TempData["HorarioMessage"] = "Horario actualizado.";
            return RedirectToAction(nameof(Index), new { plyID = model.PlyID });
        }

        public async Task<IActionResult> Delete(int plyID, int claDiasID, DateTime horFyhIni)
        {
            var dueNu = CurrentUserId();
            if (!dueNu.HasValue) return Unauthorized();

            if (!await OwnsPlayaAsync(dueNu.Value, plyID))
                return NotFound();

            var horario = await _ctx.Horarios
                .Include(h => h.Playa)
                .Include(h => h.ClasificacionDias)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.PlyID == plyID && h.ClaDiasID == claDiasID && h.HorFyhIni == horFyhIni);

            return horario is null ? NotFound() : View(horario);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int claDiasID, DateTime horFyhIni)
        {
            var dueNu = CurrentUserId();
            if (!dueNu.HasValue) return Unauthorized();

            if (!await OwnsPlayaAsync(dueNu.Value, plyID))
                return NotFound();

            var horario = await _ctx.Horarios
                .FirstOrDefaultAsync(h => h.PlyID == plyID && h.ClaDiasID == claDiasID && h.HorFyhIni == horFyhIni);

            if (horario is null) return NotFound();

            _ctx.Horarios.Remove(horario);
            await _ctx.SaveChangesAsync();

            TempData["HorarioMessage"] = "Horario eliminado.";
            return RedirectToAction(nameof(Index), new { plyID });
        }
    }
}




