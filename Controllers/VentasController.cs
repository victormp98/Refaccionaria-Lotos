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

        public IActionResult Mostrador()
        {
            return View();
        }

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

        // --- API INTERNA PARA EL TICKET (SIN RECARGAS) ---

        [HttpPost]
        public async Task<JsonResult> AgregarAlTicket(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return Json(new { success = false, message = "Producto no encontrado" });

            // Usamos una sesión DIFERENTE para el POS para no mezclar con el carrito web del usuario
            // O podemos usar la misma si quieres que se sincronicen. Usaremos la misma "Carrito" por simplicidad.
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito") ?? new List<ItemCarrito>();

            var item = carrito.FirstOrDefault(c => c.ProductoId == id);

            if (item != null)
            {
                if (item.Cantidad + 1 > producto.Stock)
                    return Json(new { success = false, message = "Stock insuficiente" });
                item.Cantidad++;
            }
            else
            {
                carrito.Add(new ItemCarrito
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    Precio = producto.PrecioVenta,
                    Cantidad = 1,
                    StockMaximo = producto.Stock,
                    ImagenUrl = producto.ImagenUrl
                });
            }

            HttpContext.Session.SetObject("Carrito", carrito);
            return Json(new { success = true });
        }

        [HttpGet]
        public JsonResult ObtenerTicket()
        {
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito") ?? new List<ItemCarrito>();
            return Json(new
            {
                items = carrito,
                total = carrito.Sum(x => x.SubTotal).ToString("N2"),
                count = carrito.Sum(x => x.Cantidad)
            });
        }

        [HttpPost]
        public JsonResult EliminarDelTicket(int id)
        {
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito");
            if (carrito != null)
            {
                var item = carrito.FirstOrDefault(c => c.ProductoId == id);
                if (item != null)
                {
                    carrito.Remove(item);
                    HttpContext.Session.SetObject("Carrito", carrito);
                }
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult LimpiarTicket()
        {
            HttpContext.Session.Remove("Carrito");
            return Json(new { success = true });
        }

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
                var pedido = new Pedido
                {
                    ClienteId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    FechaPedido = DateTime.Now,
                    Status = PedidoStatus.Entregado,
                    TotalPedido = carrito.Sum(x => x.Cantidad * x.Precio),
                    NombreReceptor = nombreCliente ?? "Público General",
                    DireccionEnvio = "Venta en Mostrador",
                    CiudadEnvio = "N/A",
                    EstadoEnvio = "N/A",
                    CodigoPostalEnvio = "00000",
                    TipoEntrega = 2,
                    RequiereFactura = !string.IsNullOrEmpty(rfc),
                    Rfc = rfc
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

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