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

        // --- SUMAR (+1) ---
        [HttpPost]
        public async Task<JsonResult> AgregarAlTicket(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return Json(new { success = false, message = "Producto no encontrado" });

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

        // --- RESTAR (-1) [NUEVO] ---
        [HttpPost]
        public JsonResult RestarDelTicket(int id)
        {
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito");
            if (carrito != null)
            {
                var item = carrito.FirstOrDefault(c => c.ProductoId == id);
                if (item != null)
                {
                    item.Cantidad--; // Restamos 1
                    if (item.Cantidad <= 0)
                    {
                        carrito.Remove(item); // Si llega a 0, lo borramos
                    }
                    HttpContext.Session.SetObject("Carrito", carrito);
                }
            }
            return Json(new { success = true });
        }

        [HttpGet]
        public JsonResult ObtenerTicket()
        {
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito") ?? new List<ItemCarrito>();
            return Json(new { items = carrito, total = carrito.Sum(x => x.SubTotal).ToString("N2"), count = carrito.Sum(x => x.Cantidad) });
        }

        [HttpPost]
        public JsonResult EliminarDelTicket(int id)
        {
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito");
            if (carrito != null)
            {
                var item = carrito.FirstOrDefault(c => c.ProductoId == id);
                if (item != null) { carrito.Remove(item); HttpContext.Session.SetObject("Carrito", carrito); }
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult LimpiarTicket()
        {
            HttpContext.Session.Remove("Carrito");
            return Json(new { success = true });
        }

        // --- FINALIZAR VENTA (AHORA CALCULA EL IVA REAL) ---
        [HttpPost]
        public async Task<IActionResult> FinalizarVenta(string nombreCliente, string metodoPago, bool aplicaIVA, string rfc = null)
        {
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito");

            if (carrito == null || !carrito.Any()) return Json(new { success = false, message = "El ticket está vacío." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Calcular totales
                decimal subtotal = carrito.Sum(x => x.Cantidad * x.Precio);
                decimal totalFinal = subtotal;
                string infoIva = " (Sin IVA)";

                // APLICAR LÓGICA DE IVA
                if (aplicaIVA)
                {
                    // Opción A: El precio YA incluye IVA, desglosamos (lo más común en retail)
                    // Opción B: Se suma el 16%. Según lo que me dijiste, quieres SUMARLO.
                    totalFinal = subtotal * 1.16m;
                    infoIva = " (Con IVA 16%)";
                }

                // 1. Crear Pedido
                var pedido = new Pedido
                {
                    ClienteId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    FechaPedido = DateTime.Now,
                    Status = PedidoStatus.Entregado,
                    TotalPedido = totalFinal, // GUARDAMOS EL TOTAL YA CON O SIN IVA
                    NombreReceptor = nombreCliente ?? "Público General",
                    DireccionEnvio = "Mostrador - " + (metodoPago ?? "Efectivo") + infoIva,
                    CiudadEnvio = "N/A",
                    EstadoEnvio = "N/A",
                    CodigoPostalEnvio = "00000",
                    TipoEntrega = 2,
                    RequiereFactura = !string.IsNullOrEmpty(rfc),
                    Rfc = rfc
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // 2. Procesar Items y Stock
                foreach (var item in carrito)
                {
                    var producto = await _context.Productos.FindAsync(item.ProductoId);
                    if (producto == null || producto.Stock < item.Cantidad)
                        throw new Exception($"Stock insuficiente para {item.Nombre}");

                    producto.Stock -= item.Cantidad;
                    _context.Update(producto);

                    _context.DetallesPedido.Add(new DetallePedido
                    {
                        PedidoId = pedido.Id,
                        ProductoId = item.ProductoId,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.Precio
                    });

                    _context.MovimientosInventario.Add(new MovimientoInventario
                    {
                        ProductoId = item.ProductoId,
                        TipoMovimiento = "Salida Venta",
                        Cantidad = item.Cantidad,
                        FechaRegistro = DateTime.Now,
                        UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)
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