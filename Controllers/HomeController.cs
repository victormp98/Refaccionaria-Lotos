using Microsoft.AspNetCore.Mvc;
using RefaccionariaWeb.Models;
using System.Diagnostics;
using RefaccionariaWeb.Services;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace RefaccionariaWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAlmacenService _almacenService;

        public HomeController(ILogger<HomeController> logger, IAlmacenService almacenService)
        {
            _logger = logger;
            _almacenService = almacenService;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            // CORRECTO: Pasamos la búsqueda al servicio para que SQL se encargue de los filtros y los nulos
            var productos = await _almacenService.ObtenerTodosLosProductos(soloVisibles: true, buscar: searchString);

            ViewData["CurrentFilter"] = searchString;
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
