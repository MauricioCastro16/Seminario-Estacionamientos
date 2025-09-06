using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _ctx;

    public HomeController(ILogger<HomeController> logger, AppDbContext ctx)
    {
        _logger = logger;
        _ctx = ctx;
    }

    public async Task<IActionResult> Index()
    {
        // Por defecto
        ViewBag.TurnoAbierto = null;

        // Solo si es Playero buscamos su turno abierto
        if (User.IsInRole("Playero"))
        {
            // MISMO claim que usa tu TurnoController
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(id, out var plaNU) && plaNU > 0)
            {
                var abierto = await _ctx.Turnos
                    .Include(t => t.Playa)
                    .AsNoTracking()
                    .Where(t => t.PlaNU == plaNU && t.TurFyhFin == null)
                    .OrderByDescending(t => t.TurFyhIni)
                    .FirstOrDefaultAsync();

                ViewBag.TurnoAbierto = abierto;
            }
        }

        return View();
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
