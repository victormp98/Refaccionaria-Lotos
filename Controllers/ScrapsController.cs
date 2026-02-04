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

        // PANEL PRINCIPAL: Buscador y tarjetas de productos
        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        // VISTA DE HISTORIAL: Lista de piezas dañadas (Historial.cshtml)
        public async Task<IActionResult> Historial()
        {
            var historial = await _context.Scraps
                .Include(s => s.Producto)
                .OrderByDescending(s => s.FechaRegistro)
                .ToListAsync();
            return View(historial);
        }

        // ACCIÓN DEL BOTÓN CONFIRMAR: Procesa el formulario del modal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reportar(int productoId, int cantidad, string motivo)
        {
            var producto = await _context.Productos.FindAsync(productoId);

            if (producto == null) return NotFound();

            if (cantidad <= 0 || cantidad > producto.Stock)
            {
                TempData["Error"] = "La cantidad es inválida o supera el stock disponible.";
                return RedirectToAction(nameof(Index));
            }

            // DESCONTAR Y REGISTRAR
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

            TempData["Success"] = "Reporte de scrap generado exitosamente.";

            // Redirigir al historial para confirmar la baja
            return RedirectToAction(nameof(Historial));
        }
    }
}