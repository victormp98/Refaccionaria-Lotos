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
        public async Task<IActionResult> Create([Bind("DireccionEnvio,CiudadEnvio,EstadoEnvio,CodigoPostalEnvio,PaisEnvio,NombreReceptor,RequiereFactura,Rfc,RazonSocial")] Pedido pedido)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito");

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
                        prod.Stock -= item.Cantidad;
                        _context.Update(prod);
                        _context.Add(new DetallePedido { PedidoId = pedido.Id, ProductoId = prod.Id, Cantidad = item.Cantidad, PrecioUnitario = item.Precio });
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
        public async Task<IActionResult> UpdateStatus(int id, PedidoStatus nuevoStatus, string returnUrl = null)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido != null)
            {
                pedido.Status = nuevoStatus;
                _context.Update(pedido);
                await _context.SaveChangesAsync();
            }

            // Si tenemos una URL de retorno (como el panel de almacén), regresamos allá
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }
        [Authorize(Roles = "Admin,Almacen")]
        public async Task<IActionResult> Almacen()
        {
            // Filtramos solo los pedidos que necesitan atención del almacén
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