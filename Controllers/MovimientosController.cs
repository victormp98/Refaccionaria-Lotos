using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using Microsoft.AspNetCore.Authorization;
using RefaccionariaWeb.Models.Enums; // Aseguramos que use los Enums

namespace RefaccionariaWeb.Controllers
{
    [Authorize(Roles = "Admin,Mostrador")] // Abrimos la puerta al Mostrador
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

            // LÓGICA DE SEGURIDAD Y FILTRADO
            if (User.IsInRole("Mostrador"))
            {
                // El Mostrador SOLO puede ver Entradas, sin importar qué pida
                query = query.Where(m => m.TipoMovimiento == TipoMovimiento.Entrada);
                ViewData["Title"] = "Historial de Entradas";
                ViewData["EsBitacoraGlobal"] = false;
            }
            else // Es Admin
            {
                if (!string.IsNullOrEmpty(tipo) && tipo == "Entrada")
                {
                    // Admin pidiendo solo entradas
                    query = query.Where(m => m.TipoMovimiento == TipoMovimiento.Entrada);
                    ViewData["Title"] = "Historial de Entradas";
                    ViewData["EsBitacoraGlobal"] = false;
                }
                else
                {
                    // Admin viendo todo (Bitácora Global)
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