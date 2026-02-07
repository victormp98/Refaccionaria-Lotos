using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using RefaccionariaWeb.Models;

namespace RefaccionariaWeb.Controllers
{
    // SEGURIDAD: Solo el Admin entra aquí.
    // Si tu rol se llama diferente (ej: "Administrador"), avísame para cambiarlo.
    [Authorize(Roles = "Admin")]
    public class CortesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CortesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Cortes/Index
        public async Task<IActionResult> Index()
        {
            // Traemos el historial ordenado por fecha (el más reciente arriba)
            var historial = await _context.CortesCaja
                .Include(c => c.Usuario) // Para ver quién abrió la caja
                .OrderByDescending(c => c.FechaApertura)
                .ToListAsync();

            return View(historial);
        }
    }
}