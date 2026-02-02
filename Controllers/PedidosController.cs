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

        // POST: Pedidos/Create (Procesa la creación de un nuevo pedido)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Create([Bind("DireccionEnvio,CiudadEnvio,EstadoEnvio,CodigoPostalEnvio,PaisEnvio,NombreReceptor,RequiereFactura,Rfc,RazonSocial")] Pedido pedido)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Challenge();
                }

                // === LÓGICA DE CREACIÓN DEL PEDIDO ===
                pedido.ClienteId = currentUser.Id;
                pedido.FechaPedido = DateTime.Now;
                pedido.Status = PedidoStatus.PendienteDePago; // Estado inicial
                
                // TODO: Aquí se calcularía el TotalPedido y se añadirían los DetallesPedido
                // Esto usualmente se haría iterando sobre los items de un carrito de compras
                // Por ahora, TotalPedido se establece a 0 y Detalles está vacío
                pedido.TotalPedido = 0; // Placeholder
                // pedido.Detalles.Add(new DetallePedido { ... }); // Ejemplo

                _context.Add(pedido);
                await _context.SaveChangesAsync();
                
                // TODO: Redirigir a una página de confirmación o de pago
                return RedirectToAction(nameof(Index));
            }
            // Si el modelo no es válido, vuelve a mostrar el formulario con errores
            return View(pedido);
        }
    }
}
