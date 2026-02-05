using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using Microsoft.AspNetCore.Authorization;

namespace RefaccionariaWeb.Controllers
{
    [Authorize(Roles = "Admin,Mostrador")]
    public class MovimientosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MovimientosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string tipo)
        {
            var query = _context.MovimientosInventario
                .Include(m => m.Producto)
                .AsQueryable();

            // LÓGICA DE SEGURIDAD Y FILTRADO (USANDO TEXTO SIMPLE)
            if (User.IsInRole("Mostrador"))
            {
                // El Mostrador SOLO ve Entradas.
                // Asumimos que en tu BD la columna se llama 'TipoMovimiento' y guarda el texto "Entrada"
                query = query.Where(m => m.TipoMovimiento == "Entrada");
                ViewData["Title"] = "Historial de Entradas";
                ViewData["EsBitacoraGlobal"] = false;
            }
            else // Es Admin
            {
                if (!string.IsNullOrEmpty(tipo) && tipo == "Entrada")
                {
                    // Admin pidiendo solo entradas
                    query = query.Where(m => m.TipoMovimiento == "Entrada");
                    ViewData["Title"] = "Historial de Entradas";
                    ViewData["EsBitacoraGlobal"] = false;
                }
                else
                {
                    // Admin viendo todo
                    ViewData["Title"] = "Bitácora Global";
                    ViewData["EsBitacoraGlobal"] = true;
                }
            }

            var movimientos = await query
                .OrderByDescending(m => m.FechaRegistro)
                .ToListAsync();

            return View(movimientos);
        }
    }
}