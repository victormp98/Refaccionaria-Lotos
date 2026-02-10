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

        // ==========================================
        // 1. VISTA PRINCIPAL (CON VALIDACIÓN DE CAJA)
        // ==========================================
        public async Task<IActionResult> Mostrador()
        {
            // BUSCAMOS SI EL USUARIO YA TIENE UNA "CUBETA" (CAJA) ABIERTA
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cajaAbierta = await _context.CortesCaja
                .Where(c => c.UsuarioId == usuarioId && c.FechaCierre == null)
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync();

            // Pasamos el ID a la vista. 
            // Si es 0, el Frontend sabrá que debe bloquear la pantalla.
            ViewBag.CajaAbiertaId = cajaAbierta?.Id ?? 0;

            return View();
        }

        // ==========================================
        // 2. GESTIÓN DE CAJA (LO NUEVO)
        // ==========================================

        [HttpPost]
        public async Task<JsonResult> AbrirCaja(decimal montoInicial)
        {
            try
            {
                var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Validar que no tenga ya una abierta
                var existe = await _context.CortesCaja.AnyAsync(c => c.UsuarioId == usuarioId && c.FechaCierre == null);
                if (existe) return Json(new { success = false, message = "Ya tienes un turno abierto." });

                var nuevaCaja = new CorteCaja
                {
                    UsuarioId = usuarioId,
                    FechaApertura = DateTime.Now,
                    MontoInicial = montoInicial,
                    FechaCierre = null // Abierta
                };

                _context.CortesCaja.Add(nuevaCaja);
                await _context.SaveChangesAsync();

                return Json(new { success = true, mensaje = "Caja abierta correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerDatosCorte()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var caja = await _context.CortesCaja
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.FechaCierre == null);

            if (caja == null) return Json(new { success = false, message = "No hay caja abierta." });

            // SUMAR VENTAS DE ESTA SESIÓN
            // Buscamos "Efectivo" o "Tarjeta" en la dirección (tu lógica actual)
            var ventasEfectivo = await _context.Pedidos
                .Where(p => p.CorteCajaId == caja.Id && p.DireccionEnvio.Contains("Efectivo"))
                .SumAsync(p => p.TotalPedido);

            var ventasTarjeta = await _context.Pedidos
                .Where(p => p.CorteCajaId == caja.Id && p.DireccionEnvio.Contains("Tarjeta"))
                .SumAsync(p => p.TotalPedido);

            return Json(new
            {
                success = true,
                inicio = caja.MontoInicial,
                efectivo = ventasEfectivo,
                tarjeta = ventasTarjeta,
                totalEsperado = caja.MontoInicial + ventasEfectivo
            });
        }

        [HttpPost]
        public async Task<JsonResult> CerrarCaja(decimal montoDeclarado)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var caja = await _context.CortesCaja
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.FechaCierre == null);

            if (caja == null) return Json(new { success = false, message = "No hay caja abierta." });

            // Recalcular montos finales
            var ventasEfectivo = await _context.Pedidos
                .Where(p => p.CorteCajaId == caja.Id && p.DireccionEnvio.Contains("Efectivo"))
                .SumAsync(p => p.TotalPedido);

            var ventasTarjeta = await _context.Pedidos
                .Where(p => p.CorteCajaId == caja.Id && p.DireccionEnvio.Contains("Tarjeta"))
                .SumAsync(p => p.TotalPedido);

            // CERRAR
            caja.TotalVentasEfectivo = ventasEfectivo;
            caja.TotalVentasTarjeta = ventasTarjeta;
            caja.MontoDeclarado = montoDeclarado;
            caja.FechaCierre = DateTime.Now;
            caja.Diferencia = montoDeclarado - (caja.MontoInicial + ventasEfectivo);

            _context.Update(caja);
            await _context.SaveChangesAsync();

            return Json(new { success = true, diferencia = caja.Diferencia });
        }


        // ==========================================
        // 3. OPERATIVA (BUSCAR Y CARRITO)
        // ==========================================

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

        [HttpPost]
        public async Task<JsonResult> AgregarAlTicket(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return Json(new { success = false, message = "Producto no encontrado" });

            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito") ?? new List<ItemCarrito>();
            var item = carrito.FirstOrDefault(c => c.ProductoId == id);

            if (item != null)
            {
                if (item.Cantidad + 1 > producto.Stock) return Json(new { success = false, message = "Stock insuficiente" });
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

        [HttpPost]
        public JsonResult RestarDelTicket(int id)
        {
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito");
            if (carrito != null)
            {
                var item = carrito.FirstOrDefault(c => c.ProductoId == id);
                if (item != null)
                {
                    item.Cantidad--;
                    if (item.Cantidad <= 0) carrito.Remove(item);
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

        // ==========================================
        // 4. FINALIZAR VENTA (FUSIÓN: CAJA + INVENTARIO)
        // ==========================================

        [HttpPost]
        public async Task<IActionResult> FinalizarVenta(string nombreCliente, string metodoPago, bool aplicaIVA, string rfc = null)
        {
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito");
            if (carrito == null || !carrito.Any()) return Json(new { success = false, message = "El ticket está vacío." });

            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // --- VALIDACIÓN DE CAJA (IMPORTANTE) ---
            var cajaAbierta = await _context.CortesCaja
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.FechaCierre == null);

            if (cajaAbierta == null)
            {
                return Json(new { success = false, message = "⚠️ ERROR: Tu turno está cerrado. Refresca para abrir caja." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal subtotal = carrito.Sum(x => x.Cantidad * x.Precio);
                decimal totalFinal = subtotal;
                string infoIva = " (Sin IVA)";

                if (aplicaIVA)
                {
                    totalFinal = subtotal * 1.16m;
                    infoIva = " (Con IVA 16%)";
                }

                // 1. Crear Pedido (CON EL ID DE LA CAJA)
                var pedido = new Pedido
                {
                    ClienteId = usuarioId,
                    FechaPedido = DateTime.Now,
                    Status = PedidoStatus.Entregado,
                    TotalPedido = totalFinal,
                    NombreReceptor = nombreCliente ?? "Público General",
                    DireccionEnvio = "Mostrador - " + (metodoPago ?? "Efectivo") + infoIva,
                    CiudadEnvio = "N/A",
                    EstadoEnvio = "N/A",
                    CodigoPostalEnvio = "00000",
                    TipoEntrega = 2,
                    RequiereFactura = !string.IsNullOrEmpty(rfc),
                    Rfc = rfc,

                    // AQUÍ ESTÁ EL ESLABÓN:
                    CorteCajaId = cajaAbierta.Id
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // 2. Procesar Items y Movimientos de Inventario (TU CÓDIGO)
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

                    // REGISTRO EN KARDEX (TU CÓDIGO CONSERVADO)
                    _context.MovimientosInventario.Add(new MovimientoInventario
                    {
                        ProductoId = item.ProductoId,
                        TipoMovimiento = "Salida Venta",
                        Cantidad = item.Cantidad,
                        FechaRegistro = DateTime.Now,
                        UsuarioId = usuarioId
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

        // ==========================================
        // 5. NUEVO MÉTODO PARA IMPRIMIR TICKET CLIENTE
        // ==========================================
        public async Task<IActionResult> ImprimirTicketVenta(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Detalles)
                    .ThenInclude(dp => dp.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            // Jalamos la configuración de la sucursal (Id 1)
            var config = await _context.SucursalConfigs.FirstOrDefaultAsync(c => c.Id == 1);

            // Si la tabla está vacía, evitamos que el sistema truene
            ViewBag.Sucursal = config ?? new SucursalConfig
            {
                NombreTienda = "REFACCIONARIA",
                Direccion = "CENTRO",
                Ciudad = "REYNOSA",
                Estado = "TAMAULIPAS",
                Telefono = "000-000-0000"
            };

            return View(pedido);
        }
    }
}