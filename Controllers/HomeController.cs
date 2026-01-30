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
        private readonly ApplicationDbContext _context; // 1. Referencia a la BD

        // 2. Inyectamos la BD en el constructor
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // 3. Enviamos la lista de productos a la vista
        public async Task<IActionResult> Index()
        {
            // Solo mostramos los que están marcados como "Visibles en Línea"
            var productos = await _context.Productos
                                          .Where(p => p.EsVisibleEnLinea == true)
                                          .ToListAsync();
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