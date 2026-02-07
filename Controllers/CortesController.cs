using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using RefaccionariaWeb.Models;

namespace RefaccionariaWeb.Controllers
{
    // SEGURIDAD: Solo el Admin entra aquí.
    [Authorize(Roles = "Admin")]
    public class CortesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CortesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Cortes/Index (LISTA GENERAL)
        public async Task<IActionResult> Index()
        {
            var historial = await _context.CortesCaja
                .Include(c => c.Usuario)
                .OrderByDescending(c => c.FechaApertura)
                .ToListAsync();

            return View(historial);
        }
        public async Task<IActionResult> ReporteGeneral()
        {
            var historial = await _context.CortesCaja
                .Include(c => c.Usuario)
                .OrderByDescending(c => c.FechaApertura)
                .ToListAsync();

            return View(historial);
        }

        // --- [NUEVO] IMPRIMIR UN SOLO TICKET ---
        // GET: /Cortes/Imprimir/5
        public async Task<IActionResult> Imprimir(int id)
        {
            var corte = await _context.CortesCaja
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (corte == null)
            {
                return NotFound();
            }

            // Usaremos una vista especial limpia para imprimir
            return View(corte);
        }
    }
}