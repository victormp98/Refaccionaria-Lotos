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
            // La llamada al servicio ya filtra por !Eliminado y EsVisibleEnLinea
            var productos = await _almacenService.ObtenerTodosLosProductos(soloVisibles: true);

            // Si el usuario escribió algo en el buscador, filtramos sobre los resultados del servicio
            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerSearchString = searchString.ToLower();
                productos = productos.Where(p =>
                    p.Nombre.ToLower().Contains(lowerSearchString) ||
                    p.MarcaPieza.ToLower().Contains(lowerSearchString) ||
                    (p.Descripcion != null && p.Descripcion.ToLower().Contains(lowerSearchString))).ToList();

                ViewData["CurrentFilter"] = searchString;
            }

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
