using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using RefaccionariaWeb.Extensions;
using RefaccionariaWeb.Models;
using RefaccionariaWeb.Models.DTOs;
using RefaccionariaWeb.Models.Enums;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using RefaccionariaWeb.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace RefaccionariaWeb.Controllers
{
    [Authorize(Roles = "Admin,Mostrador")]
    public class VentasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VentasController> _logger;
        private readonly IAlmacenService _almacenService;
        private readonly IVentasService _ventasService; // Inyección del nuevo servicio

        public VentasController(
            ApplicationDbContext context,
            ILogger<VentasController> logger,
            IAlmacenService almacenService,
            IVentasService ventasService) // Añadimos IVentasService
        {
            _context = context;
            _logger = logger;
            _almacenService = almacenService;
            _ventasService = ventasService; // Asignamos el nuevo servicio
        }

        // ==========================================
        // 1. VISTA PRINCIPAL (CON VALIDACIÓN DE CAJA)
        // ==========================================
        public async Task<IActionResult> Mostrador()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cajaAbierta = await _context.CortesCaja
                .Where(c => c.UsuarioId == usuarioId && c.FechaCierre == null)
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync();

            ViewBag.CajaAbiertaId = cajaAbierta?.Id ?? 0;

            return View();
        }

        // ==========================================
        // 2. GESTIÓN DE CAJA
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AbrirCaja(decimal montoInicial)
        {
            try
            {
                var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var existe = await _context.CortesCaja.AnyAsync(c => c.UsuarioId == usuarioId && c.FechaCierre == null);
                if (existe) return Json(new { success = false, message = "Ya tienes un turno abierto." });

                var nuevaCaja = new CorteCaja
                {
                    UsuarioId = usuarioId,
                    FechaApertura = DateTime.Now,
                    MontoInicial = montoInicial,
                    FechaCierre = null
                };

                _context.CortesCaja.Add(nuevaCaja);
                await _context.SaveChangesAsync();

                return Json(new { success = true, mensaje = "Caja abierta correctamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al abrir caja");
                return Json(new { success = false, message = "Ocurrió un error al intentar abrir la caja." });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerDatosCorte()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var caja = await _context.CortesCaja
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.FechaCierre == null);

            if (caja == null) return Json(new { success = false, message = "No hay caja abierta." });

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
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> CerrarCaja(decimal montoDeclarado)
        {
            try
            {
                var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var caja = await _context.CortesCaja
                    .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.FechaCierre == null);

                if (caja == null) return Json(new { success = false, message = "No hay caja abierta." });

                var ventasEfectivo = await _context.Pedidos
                    .Where(p => p.CorteCajaId == caja.Id && p.DireccionEnvio.Contains("Efectivo"))
                    .SumAsync(p => p.TotalPedido);

                var ventasTarjeta = await _context.Pedidos
                    .Where(p => p.CorteCajaId == caja.Id && p.DireccionEnvio.Contains("Tarjeta"))
                    .SumAsync(p => p.TotalPedido);

                caja.TotalVentasEfectivo = ventasEfectivo;
                caja.TotalVentasTarjeta = ventasTarjeta;
                caja.MontoDeclarado = montoDeclarado;
                caja.FechaCierre = DateTime.Now;
                caja.Diferencia = montoDeclarado - (caja.MontoInicial + ventasEfectivo);

                _context.Update(caja);
                await _context.SaveChangesAsync();

                return Json(new { success = true, diferencia = caja.Diferencia });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar caja");
                return Json(new { success = false, message = "Error al procesar el cierre." });
            }
        }

        // ==========================================
        // 3. OPERATIVA (BUSCAR Y CARRITO)
        // ==========================================

        [HttpGet]
        public async Task<JsonResult> BuscarProductos(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 2) return Json(new List<object>());

            var productos = await _almacenService.ObtenerTodosLosProductos(soloVisibles: false, buscar: term);

            var resultado = productos
                .Where(p => p.Stock > 0)
                .Take(10)
                .Select(p => new {
                    p.Id,
                    p.Nombre,
                    p.SKU,
                    p.MarcaPieza,
                    p.PrecioVenta,
                    p.Stock,
                    p.ImagenUrl
                });

            return Json(resultado);
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
        // 4. FINALIZAR VENTA (SIN LA VARIABLE INVENTADA)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarVenta(string nombreCliente, string metodoPago, bool aplicaIVA, string rfc = null, string razonSocial = null)
        {
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito");
            if (carrito == null || !carrito.Any()) return Json(new { success = false, message = "El ticket está vacío." });

            var empleadoId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cajaAbierta = await _context.CortesCaja
                .FirstOrDefaultAsync(c => c.UsuarioId == empleadoId && c.FechaCierre == null);

            if (cajaAbierta == null)
                return Json(new { success = false, message = "⚠️ ERROR: Tu turno está cerrado." });

            try
            {
                var pedidoId = await _ventasService.ProcesarVentaMostrador(
                    carrito: carrito,
                    empleadoId: empleadoId,
                    corteCajaId: cajaAbierta.Id,
                    nombreReceptor: nombreCliente,
                    metodoPago: metodoPago,
                    aplicaIVA: aplicaIVA,
                    rfc: rfc,
                    razonSocial: razonSocial
                );

                if (pedidoId == null)
                {
                    return Json(new { success = false, message = "Error al procesar la venta." });
                }

                HttpContext.Session.Remove("Carrito");
                return Json(new { success = true, pedidoId = pedidoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falla en transacción de venta de mostrador.");
                return Json(new { success = false, message = "Ocurrió un error interno al procesar la venta." });
            }
        }

        // ==========================================
        // 5. MÉTODO PARA IMPRIMIR TICKET CLIENTE
        // ==========================================
        public async Task<IActionResult> ImprimirTicketVenta(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Detalles)
                    .ThenInclude(dp => dp.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            var config = await _context.SucursalConfigs.FirstOrDefaultAsync(c => c.Id == 1);

            ViewBag.Sucursal = config ?? new SucursalConfig
            {
                NombreTienda = "REFACCIONARIA",
                Direccion = "CENTRO",
                Ciudad = "REYNOSA",
                Estado = "TAMAULIPAS",
                CP = "00000",
                Telefono = "000-000-0000"
            };

            return View(pedido);
        }
    }
}
