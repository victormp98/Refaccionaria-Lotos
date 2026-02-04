using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using RefaccionariaWeb.Models;
using System.Diagnostics;

namespace RefaccionariaWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // MODIFICADO: Ahora recibe el parámetro de búsqueda del Layout
        public async Task<IActionResult> Index(string searchString)
        {
            // Iniciamos la consulta con los productos visibles
            var productosQuery = _context.Productos
                                         .Where(p => p.EsVisibleEnLinea == true);

            // Si el usuario escribió algo en el buscador
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                productosQuery = productosQuery.Where(p =>
                    p.Nombre.ToLower().Contains(searchString) ||
                    p.MarcaPieza.ToLower().Contains(searchString) ||
                    (p.Descripcion != null && p.Descripcion.ToLower().Contains(searchString)));

                ViewData["CurrentFilter"] = searchString;
            }

            var productos = await productosQuery.ToListAsync();
            return View(productos);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}