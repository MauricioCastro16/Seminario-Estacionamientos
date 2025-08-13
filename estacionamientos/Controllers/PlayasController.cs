using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class PlayasController : Controller
    {
        private readonly AppDbContext _context;

        public PlayasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Playas
        public async Task<IActionResult> Index()
        {
            return View(await _context.Playas.ToListAsync());
        }

        // GET: Playas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var playaEstacionamiento = await _context.Playas
                .FirstOrDefaultAsync(m => m.Id == id);
            if (playaEstacionamiento == null)
            {
                return NotFound();
            }

            return View(playaEstacionamiento);
        }

        // GET: Playas/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Playas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Provincia,Ciudad,Direccion,TipoPiso,ValoracionPromedio,LlaveRequerida")] PlayaEstacionamiento playaEstacionamiento)
        {
            if (ModelState.IsValid)
            {
                _context.Add(playaEstacionamiento);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(playaEstacionamiento);
        }

        // GET: Playas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var playaEstacionamiento = await _context.Playas.FindAsync(id);
            if (playaEstacionamiento == null)
            {
                return NotFound();
            }
            return View(playaEstacionamiento);
        }

        // POST: Playas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Provincia,Ciudad,Direccion,TipoPiso,ValoracionPromedio,LlaveRequerida")] PlayaEstacionamiento playaEstacionamiento)
        {
            if (id != playaEstacionamiento.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(playaEstacionamiento);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PlayaEstacionamientoExists(playaEstacionamiento.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(playaEstacionamiento);
        }

        // GET: Playas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var playaEstacionamiento = await _context.Playas
                .FirstOrDefaultAsync(m => m.Id == id);
            if (playaEstacionamiento == null)
            {
                return NotFound();
            }

            return View(playaEstacionamiento);
        }

        // POST: Playas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var playaEstacionamiento = await _context.Playas.FindAsync(id);
            if (playaEstacionamiento != null)
            {
                _context.Playas.Remove(playaEstacionamiento);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PlayaEstacionamientoExists(int id)
        {
            return _context.Playas.Any(e => e.Id == id);
        }
    }
}
