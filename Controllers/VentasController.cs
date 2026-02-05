using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using RefaccionariaWeb.Extensions;
using RefaccionariaWeb.Models;
using RefaccionariaWeb.Models.DTOs;
using RefaccionariaWeb.Models.Enums;
using System.Security.Claims;

namespace RefaccionariaWeb.Controllers
{
    [Authorize(Roles = "Admin,Mostrador")]
    public class VentasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VentasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Vista Principal del POS
        public IActionResult Mostrador()
        {
            return View();
        }

        // BUSCADOR NINJA (AJAX)
        [HttpGet]
        public async Task<JsonResult> BuscarProductos(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 2) return Json(new List<object>());

            var productos = await _context.Productos
                .Where(p => p.EsVisibleEnLinea && p.Stock > 0 &&
                           (p.Nombre.Contains(term) || p.SKU.Contains(term) || p.MarcaPieza.Contains(term)))
                .Take(10)
                .Select(p => new {
                    p.Id,
                    p.Nombre,
                    p.SKU,
                    p.MarcaPieza,
                    p.PrecioVenta,
                    p.Stock,
                    p.ImagenUrl
                })
                .ToListAsync();

            return Json(productos);
        }

        // FINALIZAR VENTA (MOSTRADOR)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarVenta(string nombreCliente, string rfc = null)
        {
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito");

            if (carrito == null || !carrito.Any())
            {
                return Json(new { success = false, message = "El ticket está vacío." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Crear el Pedido
                var pedido = new Pedido
                {
                    ClienteId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    FechaPedido = DateTime.Now,
                    Status = PedidoStatus.Entregado, // Venta en mostrador se marca como entregada
                    TotalPedido = carrito.Sum(x => x.Cantidad * x.Precio),
                    NombreReceptor = nombreCliente ?? "Público General",
                    DireccionEnvio = "Venta en Mostrador",
                    CiudadEnvio = "N/A",
                    EstadoEnvio = "N/A",
                    CodigoPostalEnvio = "00000",
                    TipoEntrega = 2, // 2 = Venta Mostrador
                    RequiereFactura = !string.IsNullOrEmpty(rfc),
                    Rfc = rfc
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // 2. Detalles y Stock
                foreach (var item in carrito)
                {
                    var producto = await _context.Productos.FindAsync(item.ProductoId);
                    if (producto == null || producto.Stock < item.Cantidad)
                        throw new Exception($"Stock insuficiente para {item.Nombre}");

                    producto.Stock -= item.Cantidad;
                    _context.DetallesPedido.Add(new DetallePedido
                    {
                        PedidoId = pedido.Id,
                        ProductoId = item.ProductoId,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.Precio
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Limpiar carrito de mostrador
                HttpContext.Session.Remove("Carrito");

                return Json(new { success = true, pedidoId = pedido.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}