using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using RefaccionariaWeb.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace RefaccionariaWeb.Controllers
{
    [Authorize(Roles = "Admin,Almacen")]
    public class EntradasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EntradasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarEntrada(int productoId, int cantidad, string referencia)
        {
            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null) return NotFound();

            if (cantidad <= 0)
            {
                TempData["Error"] = "La cantidad debe ser mayor a cero.";
                return RedirectToAction(nameof(Index));
            }

            // SUMAR STOCK
            producto.Stock += cantidad;

            // REGISTRAR EN BITÁCORA
            var movimiento = new MovimientoInventario
            {
                ProductoId = productoId,
                TipoMovimiento = "ENTRADA",
                Cantidad = cantidad,
                FechaRegistro = DateTime.Now,
                Referencia = referencia ?? "Entrada manual de almacén",
                UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                NombreUsuario = User.Identity?.Name
            };

            _context.MovimientosInventario.Add(movimiento);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Stock actualizado: +{cantidad} unidades a {producto.Nombre}.";
            return RedirectToAction(nameof(Index));
        }


        // Acción para ver solo el historial de entradas
        public async Task<IActionResult> Historial()
        {
            var historial = await _context.MovimientosInventario
                .Include(m => m.Producto)
                .Where(m => m.TipoMovimiento == "ENTRADA") // Filtramos solo entradas
                .OrderByDescending(m => m.FechaRegistro)
                .ToListAsync();

            return View(historial);
        }
    }
}