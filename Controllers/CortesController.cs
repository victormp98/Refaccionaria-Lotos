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
        public async Task<IActionResult> ReporteGeneral(DateTime? inicio = null, DateTime? fin = null)
        {
            var query = _context.CortesCaja.Include(c => c.Usuario).AsQueryable();

            if (inicio.HasValue) 
                query = query.Where(c => c.FechaApertura >= inicio.Value);
            
            if (fin.HasValue) 
                query = query.Where(c => c.FechaApertura <= fin.Value.AddDays(1));

            var historial = await query.OrderByDescending(c => c.FechaApertura).ToListAsync();

            ViewData["FechaInicio"] = inicio?.ToString("yyyy-MM-dd");
            ViewData["FechaFin"] = fin?.ToString("yyyy-MM-dd");

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