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
                setTempDataError("La cantidad es inválida o supera el stock disponible.");
                return RedirectToAction(nameof(Index));
            }

            // 1. DESCONTAR DEL STOCK PRINCIPAL
            producto.Stock -= cantidad;

            // 2. REGISTRAR EN LA TABLA DE SCRAPS (Historial de daños)
            var scrap = new Scrap
            {
                ProductoId = productoId,
                Cantidad = cantidad,
                Motivo = motivo,
                FechaRegistro = DateTime.Now,
                UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                NombreUsuario = User.Identity?.Name
            };

            // 3. REGISTRAR EN LA BITÁCORA GLOBAL DE MOVIMIENTOS
            // Esto es lo que acabamos de crear para que el Admin tenga el rastro completo
            var movimiento = new MovimientoInventario
            {
                ProductoId = productoId,
                TipoMovimiento = "SCRAP", // Identificador de tipo de movimiento
                Cantidad = -cantidad,    // Guardamos en negativo porque es una salida
                FechaRegistro = DateTime.Now,
                Referencia = $"Motivo: {motivo}", // Para que el Admin sepa por qué fue
                UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                NombreUsuario = User.Identity?.Name
            };

            _context.Scraps.Add(scrap);
            _context.MovimientosInventario.Add(movimiento); // Guardamos en la tabla nueva

            await _context.SaveChangesAsync();

            TempData["Success"] = "Reporte de scrap generado y registrado en bitácora.";

            // Redirigir al historial para confirmar la baja
            return RedirectToAction(nameof(Historial));
        }

        // Función auxiliar para mantener consistencia si la llegaras a ocupar
        private void setTempDataError(string mensaje)
        {
            TempData["Error"] = mensaje;
        }
    }
}