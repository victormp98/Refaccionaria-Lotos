using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RefaccionariaWeb.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Security.Claims;
using RefaccionariaWeb.Services;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;

namespace RefaccionariaWeb.Controllers
{
    [Authorize(Roles = "Admin,Mostrador,Almacen")]
    public class ProductosController : Controller
    {
        private readonly IAlmacenService _almacenService;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductosController(IAlmacenService almacenService, IWebHostEnvironment hostEnvironment)
        {
            _almacenService = almacenService;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            var productos = await _almacenService.ObtenerTodosLosProductos(soloVisibles: false); // Obtiene todos los productos no eliminados para la búsqueda

            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerSearchString = searchString.ToLower();
                productos = productos.Where(p =>
                    p.Nombre.ToLower().Contains(lowerSearchString) ||
                    p.SKU.ToLower().Contains(lowerSearchString) ||
                    (p.MarcaPieza != null && p.MarcaPieza.ToLower().Contains(lowerSearchString))).ToList();

                ViewData["CurrentFilter"] = searchString;
            }

            return View(productos);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var producto = await _almacenService.ObtenerProductoPorId(id.Value);
            if (producto == null) return NotFound();
            return View(producto);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Producto producto, IFormFile? imagenArchivo)
        {
            if (imagenArchivo != null && imagenArchivo.Length > 0)
            {
                producto.ImagenUrl = await GuardarImagen(imagenArchivo);
            }

            if (ModelState.IsValid)
            {
                await _almacenService.CrearProducto(producto);
                return RedirectToAction(nameof(Index));
            }
            return View(producto);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var producto = await _almacenService.ObtenerProductoPorId(id.Value);
            if (producto == null) return NotFound();
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Producto producto, IFormFile? imagenArchivo)
        {
            if (id != producto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                if (imagenArchivo != null && imagenArchivo.Length > 0)
                {
                    producto.ImagenUrl = await GuardarImagen(imagenArchivo);
                }

                var resultado = await _almacenService.EditarProducto(producto);
                if (!resultado)
                {
                    return NotFound();
                }
                return RedirectToAction(nameof(Index));
            }
            return View(producto);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] // Solo el Admin puede alternar visibilidad
        public async Task<IActionResult> AlternarVisibilidadWeb(int id)
        {
            await _almacenService.AlternarVisibilidadWeb(id);
            return RedirectToAction(nameof(Index)); // Redirige de vuelta al listado
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Compra(int? id)
        {
            if (id == null) return NotFound();
            var producto = await _almacenService.ObtenerProductoPorId(id.Value);
            if (producto == null) return NotFound();
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Compra(int id, int cantidad, decimal precioCompra, decimal precioVenta, string? referencia)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var resultado = await _almacenService.RegistrarCompra(id, cantidad, precioCompra, precioVenta, usuarioId, referencia);

            if (!resultado)
            {
                ModelState.AddModelError("", "No se pudo registrar la compra. Verifique que la cantidad sea válida, el producto exista o el precio de venta no sea menor al de compra.");
                var producto = await _almacenService.ObtenerProductoPorId(id);
                return View(producto);
            }

            TempData["Success"] = $"Compra registrada: {cantidad} unidades.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var producto = await _almacenService.ObtenerProductoPorId(id.Value);
            if (producto == null) return NotFound();
            return View(producto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _almacenService.MoverAPapelera(id);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Papelera()
        {
            var productosOcultos = await _almacenService.ObtenerPapelera();
            return View(productosOcultos);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restaurar(int id)
        {
            await _almacenService.RestaurarDePapelera(id);
            return RedirectToAction(nameof(Papelera));
        }

        private async Task<string> GuardarImagen(IFormFile archivo)
        {
            var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);
            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "imagenes");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            string rutaGuardado = Path.Combine(uploadsFolder, nombreArchivo);
            using (var stream = new FileStream(rutaGuardado, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }
            return "/imagenes/" + nombreArchivo;
        }
    }
}
