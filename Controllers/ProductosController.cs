using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using RefaccionariaWeb.Models;
using Microsoft.AspNetCore.Hosting; // Obligatorio
using System.IO;

namespace RefaccionariaWeb.Controllers
{
    [Authorize(Roles = "Admin,Mostrador,Almacen")]
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductosController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // Si tu menú dice "Inventario" pero apunta a /Productos, este es el método que busca
        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var producto = await _context.Productos
                .Include(p => p.Compatibilidades)
                .ThenInclude(c => c.Vehiculo)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,SKU,Nombre,Descripcion,MarcaPieza,PrecioVenta,PrecioCompra,Stock,Pasillo,Anaquel,EsVisibleEnLinea")] Producto producto, IFormFile? imagenArchivo)
        {
            if (imagenArchivo != null && imagenArchivo.Length > 0)
            {
                var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(imagenArchivo.FileName);
                // ESTO ES LO QUE ARREGLA EL 404 DE LA IMAGEN Y EL ERROR DE RUTA
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "imagenes");

                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string rutaGuardado = Path.Combine(uploadsFolder, nombreArchivo);

                using (var stream = new FileStream(rutaGuardado, FileMode.Create))
                {
                    await imagenArchivo.CopyToAsync(stream);
                }
                producto.ImagenUrl = "/imagenes/" + nombreArchivo;
            }

            if (ModelState.IsValid)
            {
                _context.Add(producto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(producto);
        }

        // Asegúrate de que este método exista para evitar el 404 al editar
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();
            return View(producto);
        }
    }
}