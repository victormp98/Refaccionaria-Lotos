using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using RefaccionariaWeb.Models;
using RefaccionariaWeb.Models.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks;
using RefaccionariaWeb.Models.DTOs;
using RefaccionariaWeb.Extensions;
using Stripe.Checkout;

namespace RefaccionariaWeb.Controllers
{
    [Authorize]
    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PedidosController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            IQueryable<Pedido> pedidosQuery = _context.Pedidos
                .Include(p => p.Detalles).ThenInclude(d => d.Producto)
                .Include(p => p.Cliente);

            if (!(User.IsInRole("Admin") || User.IsInRole("Mostrador") || User.IsInRole("Almacen")))
            {
                pedidosQuery = pedidosQuery.Where(p => p.ClienteId == currentUser.Id);
            }

            var pedidos = await pedidosQuery.OrderByDescending(p => p.FechaPedido).ToListAsync();
            return View(pedidos);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var pedidoQuery = _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Detalles).ThenInclude(d => d.Producto)
                .Where(m => m.Id == id);

            if (!(User.IsInRole("Admin") || User.IsInRole("Mostrador") || User.IsInRole("Almacen")))
            {
                pedidoQuery = pedidoQuery.Where(m => m.ClienteId == (currentUser != null ? currentUser.Id : string.Empty));
            }

            var pedido = await pedidoQuery.FirstOrDefaultAsync();
            if (pedido == null) return NotFound();

            return View(pedido);
        }

        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var pedido = new CheckoutViewModel
            {
                NombreReceptor = currentUser?.UserName ?? currentUser?.Email ?? string.Empty
            };
            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Create(CheckoutViewModel pedido)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito");

            if (!pedido.RequiereFactura)
            {
                pedido.Rfc = null;
                pedido.RazonSocial = null;
            }
            else
            {
                pedido.Rfc = string.IsNullOrWhiteSpace(pedido.Rfc) ? null : pedido.Rfc.Trim();
                pedido.RazonSocial = string.IsNullOrWhiteSpace(pedido.RazonSocial) ? null : pedido.RazonSocial.Trim();
            }

            if (ModelState.IsValid && carrito != null && carrito.Any())
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var nuevoPedido = new Pedido
                    {
                        ClienteId = currentUser?.Id ?? string.Empty,
                        FechaPedido = DateTime.Now,
                        Status = PedidoStatus.PendienteDePago,
                        CorteCajaId = null,
                        DireccionEnvio = pedido.DireccionEnvio,
                        CiudadEnvio = pedido.CiudadEnvio,
                        EstadoEnvio = pedido.EstadoEnvio,
                        CodigoPostalEnvio = pedido.CodigoPostalEnvio,
                        PaisEnvio = "México",
                        NombreReceptor = pedido.NombreReceptor,
                        RequiereFactura = pedido.RequiereFactura,
                        Rfc = pedido.Rfc,
                        RazonSocial = pedido.RazonSocial,
                        TipoEntrega = pedido.TipoEntrega,
                        Detalles = new List<DetallePedido>()
                    };

                    decimal total = 0;
                    foreach (var item in carrito)
                    {
                        var prod = await _context.Productos.FindAsync(item.ProductoId);
                        if (prod == null || prod.Stock < item.Cantidad) throw new Exception("Error de stock");

                        // A) Restar Stock
                        prod.Stock -= item.Cantidad;
                        _context.Update(prod);

                        // B) Agregar Detalle
                        nuevoPedido.Detalles.Add(new DetallePedido { ProductoId = prod.Id, Cantidad = item.Cantidad, PrecioUnitario = item.Precio });

                        // C) REGISTRAR EN BITÁCORA (Salida por Venta Web)
                        _context.MovimientosInventario.Add(new MovimientoInventario
                        {
                            ProductoId = prod.Id,
                            TipoMovimiento = "Salida Venta Web", // Diferenciamos la venta web
                            Cantidad = -item.Cantidad,
                            FechaRegistro = DateTime.Now,
                            UsuarioId = currentUser?.Id ?? string.Empty,
                            Referencia = "Venta Web"
                        });

                        total += (item.Cantidad * item.Precio);
                    }

                    nuevoPedido.TotalPedido = total;
                    _context.Pedidos.Add(nuevoPedido);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // ====================================================
                    // STRIPE: CREAR PASARELA DE PAGO CHECKOUT SESSION
                    // ====================================================
                    var domain = $"{Request.Scheme}://{Request.Host}";
                    var options = new SessionCreateOptions
                    {
                        PaymentMethodTypes = new List<string> { "card" },
                        LineItems = new List<SessionLineItemOptions>(),
                        Mode = "payment",
                        SuccessUrl = domain + $"/Pedidos/PagoExitoso?id={nuevoPedido.Id}",
                        CancelUrl = domain + $"/Pedidos/PagoCancelado?id={nuevoPedido.Id}",
                        Metadata = new Dictionary<string, string>
                        {
                            { "PedidoId", nuevoPedido.Id.ToString() }
                        }
                    };

                    foreach (var item in carrito)
                    {
                        var sessionListItem = new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)(item.Precio * 100), // Stripe usa centavos
                                Currency = "mxn",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = string.IsNullOrEmpty(item.Nombre) ? "Producto" : item.Nombre
                                }
                            },
                            Quantity = item.Cantidad
                        };
                        options.LineItems.Add(sessionListItem);
                    }

                    var service = new SessionService();
                    var session = await service.CreateAsync(options);

                    HttpContext.Session.Remove("Carrito");

                    Response.Headers.Add("Location", session.Url);
                    return new StatusCodeResult(303);
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Ops, alguien compró los últimos productos justo antes que tú. Revisa tu carrito.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Error al procesar: " + ex.Message);
                }
            }
            return View(pedido);
        }

        // ... (UpdateStatus, ConfirmarEntrega, ReportarScrap, Almacen -> SIN CAMBIOS)
        [HttpPost][ValidateAntiForgeryToken][Authorize(Roles = "Admin,Mostrador,Almacen")] public async Task<IActionResult> UpdateStatus(int id, PedidoStatus nuevoStatus, string paqueteria = null, string guia = null, string returnUrl = null) { var pedido = await _context.Pedidos.FindAsync(id); if (pedido != null) { pedido.Status = nuevoStatus; if (nuevoStatus == PedidoStatus.Enviado) { pedido.Paqueteria = paqueteria; pedido.NumeroGuia = guia; pedido.FechaEnvio = DateTime.Now; } _context.Update(pedido); await _context.SaveChangesAsync(); } if (!string.IsNullOrEmpty(returnUrl)) { return Redirect(returnUrl); } return RedirectToAction(nameof(Details), new { id = id }); }
        [HttpPost][ValidateAntiForgeryToken][Authorize(Roles = "Cliente")] public async Task<IActionResult> ConfirmarEntrega(int id) { var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id); var currentUser = await _userManager.GetUserAsync(User); if (pedido == null || currentUser == null || pedido.ClienteId != currentUser.Id) return NotFound(); if (pedido.Status == PedidoStatus.Enviado) { pedido.Status = PedidoStatus.Entregado; _context.Update(pedido); await _context.SaveChangesAsync(); TempData["Success"] = "¡Gracias por confirmar la recepción de tu pedido!"; } return RedirectToAction(nameof(Details), new { id = id }); }
        [HttpPost][ValidateAntiForgeryToken][Authorize(Roles = "Admin,Almacen")] public async Task<IActionResult> ReportarScrap(int id, string motivo) { var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id); if (pedido == null) return NotFound(); pedido.Status = PedidoStatus.Cancelado; _context.Update(pedido); await _context.SaveChangesAsync(); TempData["Error"] = $"Pedido #{id} marcado como SCRAP: {motivo}"; return RedirectToAction(nameof(Almacen)); }
        [Authorize(Roles = "Admin,Almacen")] public async Task<IActionResult> Almacen() { var pedidosAlmacen = await _context.Pedidos.Include(p => p.Cliente).Include(p => p.Detalles).ThenInclude(d => d.Producto).Where(p => p.Status == PedidoStatus.Pagado || p.Status == PedidoStatus.EnProceso).OrderByDescending(p => p.FechaPedido).ToListAsync(); return View(pedidosAlmacen); }

        // =========================================
        // WEBHOOKS Y PANTALLAS DE STRIPE
        // =========================================

        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> PagoExitoso(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            var currentUser = await _userManager.GetUserAsync(User);
            if (pedido == null || currentUser == null || pedido.ClienteId != currentUser.Id) return NotFound();

            // Cambiamos el estado asumiendo que el pago pasó
            if (pedido.Status == PedidoStatus.PendienteDePago)
            {
                pedido.Status = PedidoStatus.Pagado; 
                _context.Update(pedido);
                await _context.SaveChangesAsync();
            }

            return View(pedido);
        }

        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> PagoCancelado(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            var currentUser = await _userManager.GetUserAsync(User);
            if (pedido == null || currentUser == null || pedido.ClienteId != currentUser.Id) return NotFound();

            return View(pedido);
        }
    }
}