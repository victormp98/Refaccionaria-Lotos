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
    [Authorize] // Asegura que solo usuarios autenticados puedan acceder a los pedidos
    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PedidosController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Pedidos (Muestra los pedidos del usuario actual)
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge(); // Redirige a login si no hay usuario
            }

            IQueryable<Pedido> pedidosQuery = _context.Pedidos
                                                     .Include(p => p.Detalles)
                                                         .ThenInclude(d => d.Producto);

            if (User.IsInRole("Admin") || User.IsInRole("Mostrador") || User.IsInRole("Almacen"))
            {
                // El personal puede ver todos los pedidos
                // No se aplica filtro por ClienteId
            }
            else // Asumimos que es un Cliente o cualquier otro rol no listado que solo ve sus propios pedidos
            {
                pedidosQuery = pedidosQuery.Where(p => p.ClienteId == currentUser.Id);
            }

            var pedidos = await pedidosQuery.ToListAsync();
            return View(pedidos);
        }

        // GET: Pedidos/Details/5 (Muestra los detalles de un pedido específico)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var pedidoQuery = _context.Pedidos
                                      .Include(p => p.Detalles)
                                          .ThenInclude(d => d.Producto)
                                      .Where(m => m.Id == id); // Filtra por ID de pedido

            if (!(User.IsInRole("Admin") || User.IsInRole("Mostrador") || User.IsInRole("Almacen")))
            {
                // Si NO es personal, filtra también por ClienteId
                pedidoQuery = pedidoQuery.Where(m => m.ClienteId == currentUser.Id);
            }

            var pedido = await pedidoQuery.FirstOrDefaultAsync();
            
            if (pedido == null)
            {
                // Si el pedido no existe o no pertenece al usuario actual (si es Cliente), no se encuentra
                return NotFound();
            }

            return View(pedido);
        }

        // GET: Pedidos/Create (Muestra el formulario para crear un nuevo pedido - desde el carrito)
        // Esta acción GET podría no ser directamente usada si el pedido se crea desde un proceso de checkout
        // Aquí podríamos mostrar un resumen del carrito antes de confirmar el pedido
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Create() // Made async to get current user
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Inicializar un nuevo pedido para la vista
            var pedido = new Pedido
            {
                ClienteId = currentUser.Id,
                // Puedes precargar datos del usuario si tienes un perfil más detallado
                // Por ejemplo, si tu IdentityUser tiene propiedades de dirección.
                // Por ahora, solo inicializamos lo básico.
                NombreReceptor = currentUser.UserName ?? currentUser.Email // Asumir username o email como nombre
            };

            // TODO: Aquí se cargaría la información del carrito de compras del usuario
            // Y se pasaría a la vista para su confirmación
            return View(pedido);
        }

        // POST: Pedidos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Create([Bind("DireccionEnvio,CiudadEnvio,EstadoEnvio,CodigoPostalEnvio,PaisEnvio,NombreReceptor,RequiereFactura,Rfc,RazonSocial")] Pedido pedido)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            // Recuperamos el carrito de la sesión
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito");

            if (currentUser == null) return Challenge();

            // Validación: No dejar comprar si el carrito está vacío o nulo
            if (carrito == null || !carrito.Any())
            {
                ModelState.AddModelError("", "Tu carrito está vacío.");
                // Regresamos a la vista mostrando el error
                return View(pedido);
            }

            // --- LIMPIEZA DE VALIDACIONES INTERNAS ---
            // Estos campos NO vienen del formulario, los calculamos nosotros.
            // Los removemos del ModelState para que no bloqueen la entrada.
            ModelState.Remove("ClienteId");
            ModelState.Remove("Cliente");       // Propiedad de navegación
            ModelState.Remove("TotalPedido");
            ModelState.Remove("FechaPedido");
            ModelState.Remove("Status");
            ModelState.Remove("Detalles");      // Lista de detalles
            // -----------------------------------------
            if (ModelState.IsValid)
            {
                // INICIO DE TRANSACCIÓN: Todo o Nada
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    // 1. Configurar datos del Pedido
                    pedido.ClienteId = currentUser.Id;
                    pedido.FechaPedido = DateTime.Now;
                    pedido.Status = PedidoStatus.PendienteDePago;
                    pedido.TotalPedido = 0; // Se calcula sumando los detalles
                    
                    _context.Add(pedido);
                    await _context.SaveChangesAsync(); // Guardamos para generar el pedido.Id

                    decimal totalCalculado = 0;

                    // 2. Procesar cada item del carrito
                    foreach (var item in carrito)
                    {
                        // Buscamos el producto real para descontar stock
                        var productoDb = await _context.Productos.FindAsync(item.ProductoId);

                        // Validación de Stock en tiempo real
                        if (productoDb == null)
                        {
                             throw new Exception($"El producto '{item.Nombre}' ya no existe.");
                        }
                        if (productoDb.Stock < item.Cantidad)
                        {
                             throw new Exception($"Stock insuficiente para '{item.Nombre}'. Disponibles: {productoDb.Stock}.");
                        }

                        // RESTAR STOCK
                        productoDb.Stock -= item.Cantidad;
                        _context.Update(productoDb);

                        // CREAR DETALLE
                        var detalle = new DetallePedido
                        {
                            PedidoId = pedido.Id,
                            ProductoId = item.ProductoId,
                            Cantidad = item.Cantidad,
                            PrecioUnitario = item.Precio
                        };
                        _context.Add(detalle);

                        totalCalculado += (item.Cantidad * item.Precio);
                    }

                    // 3. Actualizar Total y Cerrar
                    pedido.TotalPedido = totalCalculado;
                    _context.Update(pedido);
                    await _context.SaveChangesAsync();

                    // CONFIRMAR TRANSACCIÓN
                    await transaction.CommitAsync();

                    // 4. Limpiar Carrito (Ya se compró)
                    HttpContext.Session.Remove("Carrito");

                    return RedirectToAction(nameof(Details), new { id = pedido.Id });
                }
                catch (Exception ex)
                {
                    // SI ALGO FALLA: Deshacer todo
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", $"Error en la compra: {ex.Message}");
                }
            }

            return View(pedido);
        }
    }
}
