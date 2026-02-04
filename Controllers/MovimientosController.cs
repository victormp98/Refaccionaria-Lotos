using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using Microsoft.AspNetCore.Authorization;

namespace RefaccionariaWeb.Controllers
{
    [Authorize(Roles = "Admin")] // Solo el mero jefe ve la bitácora global
    public class MovimientosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MovimientosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Traemos los movimientos con los datos del producto
            var movimientos = await _context.MovimientosInventario
                .Include(m => m.Producto)
                .OrderByDescending(m => m.FechaRegistro)
                .ToListAsync();

            return View(movimientos);
        }
    }
}