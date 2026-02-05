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
using RefaccionariaWeb.Models.DTOs;
using RefaccionariaWeb.Extensions;

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
                pedidoQuery = pedidoQuery.Where(m => m.ClienteId == currentUser.Id);
            }

            var pedido = await pedidoQuery.FirstOrDefaultAsync();
            if (pedido == null) return NotFound();

            return View(pedido);
        }

        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var pedido = new Pedido
            {
                ClienteId = currentUser.Id,
                NombreReceptor = currentUser.UserName ?? currentUser.Email
            };
            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Create([Bind("DireccionEnvio,CiudadEnvio,EstadoEnvio,CodigoPostalEnvio,PaisEnvio,NombreReceptor,RequiereFactura,Rfc,RazonSocial,TipoEntrega")] Pedido pedido)
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

            ModelState.Remove("ClienteId");
            ModelState.Remove("Cliente");
            ModelState.Remove("TotalPedido");
            ModelState.Remove("FechaPedido");
            ModelState.Remove("Status");
            ModelState.Remove("Detalles");

            if (ModelState.IsValid && carrito != null && carrito.Any())
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    pedido.ClienteId = currentUser.Id;
                    pedido.FechaPedido = DateTime.Now;
                    pedido.Status = PedidoStatus.PendienteDePago;
                    _context.Add(pedido);
                    await _context.SaveChangesAsync();

                    decimal total = 0;
                    foreach (var item in carrito)
                    {
                        var prod = await _context.Productos.FindAsync(item.ProductoId);
                        if (prod == null || prod.Stock < item.Cantidad) throw new Exception("Error de stock");

                        // A) Restar Stock
                        prod.Stock -= item.Cantidad;
                        _context.Update(prod);

                        // B) Crear Detalle
                        _context.Add(new DetallePedido { PedidoId = pedido.Id, ProductoId = prod.Id, Cantidad = item.Cantidad, PrecioUnitario = item.Precio });

                        // C) 🟢 REGISTRAR EN BITÁCORA (Salida por Venta Web) 🟢
                        _context.MovimientosInventario.Add(new MovimientoInventario
                        {
                            ProductoId = prod.Id,
                            TipoMovimiento = "Salida", // Salida de inventario
                            Cantidad = item.Cantidad,
                            FechaRegistro = DateTime.Now,
                            UsuarioId = currentUser.Id, // El cliente que compró (o null si prefieres que sea system)
                            Comentarios = $"Venta Web Folio #{pedido.Id}"
                        });

                        total += (item.Cantidad * item.Precio);
                    }

                    pedido.TotalPedido = total;
                    _context.Update(pedido);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    HttpContext.Session.Remove("Carrito");
                    return RedirectToAction(nameof(Details), new { id = pedido.Id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Mostrador,Almacen")]
        public async Task<IActionResult> UpdateStatus(int id, PedidoStatus nuevoStatus, string paqueteria = null, string guia = null, string returnUrl = null)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido != null)
            {
                pedido.Status = nuevoStatus;

                if (nuevoStatus == PedidoStatus.Enviado)
                {
                    pedido.Paqueteria = paqueteria;
                    pedido.NumeroGuia = guia;
                    pedido.FechaEnvio = DateTime.Now;
                }

                _context.Update(pedido);
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> ConfirmarEntrega(int id)
        {
            var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            var currentUser = await _userManager.GetUserAsync(User);

            if (pedido == null || pedido.ClienteId != currentUser.Id) return NotFound();

            if (pedido.Status == PedidoStatus.Enviado)
            {
                pedido.Status = PedidoStatus.Entregado;
                _context.Update(pedido);
                await _context.SaveChangesAsync();
                TempData["Success"] = "¡Gracias por confirmar la recepción de tu pedido!";
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Almacen")]
        public async Task<IActionResult> ReportarScrap(int id, string motivo)
        {
            var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null) return NotFound();

            pedido.Status = PedidoStatus.Cancelado;
            _context.Update(pedido);
            await _context.SaveChangesAsync();

            TempData["Error"] = $"Pedido #{id} marcado como SCRAP: {motivo}";
            return RedirectToAction(nameof(Almacen));
        }

        [Authorize(Roles = "Admin,Almacen")]
        public async Task<IActionResult> Almacen()
        {
            var pedidosAlmacen = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Detalles).ThenInclude(d => d.Producto)
                .Where(p => p.Status == PedidoStatus.Pagado || p.Status == PedidoStatus.EnProceso)
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();

            return View(pedidosAlmacen);
        }
    }
}