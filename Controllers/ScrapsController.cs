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

        // Listado de historial de Scrap
        public async Task<IActionResult> Index()
        {
            var historial = await _context.Scraps
                .Include(s => s.Producto)
                .OrderByDescending(s => s.FechaRegistro)
                .ToListAsync();
            return View(historial);
        }

        // Vista para buscar producto y escrapear
        public async Task<IActionResult> Seleccionar()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reportar(int productoId, int cantidad, string motivo)
        {
            var producto = await _context.Productos.FindAsync(productoId);

            if (producto == null || cantidad <= 0 || string.IsNullOrEmpty(motivo))
            {
                return BadRequest("Datos inválidos");
            }

            if (cantidad > producto.Stock)
            {
                TempData["Error"] = "No puedes escrapear más de lo que hay en stock.";
                return RedirectToAction(nameof(Seleccionar));
            }

            // 1. DESCONTAR DEL STOCK
            producto.Stock -= cantidad;

            // 2. CREAR EL REGISTRO DE SCRAP
            var scrap = new Scrap
            {
                ProductoId = productoId,
                Cantidad = cantidad,
                Motivo = motivo,
                FechaRegistro = DateTime.Now,
                UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                NombreUsuario = User.Identity?.Name
            };

            _context.Add(scrap);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Se han reportado {cantidad} unidades de {producto.Nombre} como scrap.";

            return RedirectToAction(nameof(Index));
        }
    }
}