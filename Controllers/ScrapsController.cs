using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using RefaccionariaWeb.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace RefaccionariaWeb.Controllers
{
    [Authorize(Roles = "Admin,Almacen")]
    public class ScrapsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScrapsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Ver el historial de piezas dañadas
        public async Task<IActionResult> Index()
        {
            var historial = await _context.Scraps
                .Include(s => s.Producto)
                .OrderByDescending(s => s.FechaRegistro)
                .ToListAsync();
            return View(historial);
        }

        // 2. Vista para buscar el producto que se va a escrapear
        public async Task<IActionResult> Seleccionar()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        // 3. Acción que procesa el reporte y descuenta stock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reportar(int productoId, int cantidad, string motivo)
        {
            var producto = await _context.Productos.FindAsync(productoId);

            if (producto == null) return NotFound();

            if (cantidad <= 0 || cantidad > producto.Stock)
            {
                TempData["Error"] = "Cantidad inválida o superior al stock disponible.";
                return RedirectToAction(nameof(Seleccionar));
            }

            // PROCESO DINAMITA: Descontar y registrar
            producto.Stock -= cantidad;

            var scrap = new Scrap
            {
                ProductoId = productoId,
                Cantidad = cantidad,
                Motivo = motivo,
                FechaRegistro = DateTime.Now,
                UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                NombreUsuario = User.Identity?.Name
            };

            _context.Scraps.Add(scrap);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Reporte guardado. Se descontaron {cantidad} unidades de {producto.Nombre}.";
            return RedirectToAction(nameof(Index));
        }
    }
}