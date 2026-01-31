using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using RefaccionariaWeb.Models;
using Microsoft.AspNetCore.Hosting;
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

        public async Task<IActionResult> Index()
        {
            return View(await _context.Productos.ToListAsync());
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos
                .Include(p => p.Compatibilidades.Where(c => c.Vehiculo.Activo == true)) // FILTRO PARA NO MOSTRAR AUTOS BORRADOS
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
        public async Task<IActionResult> Create(Producto producto, IFormFile? imagenArchivo)
        {
            if (imagenArchivo != null && imagenArchivo.Length > 0)
            {
                producto.ImagenUrl = await GuardarImagen(imagenArchivo);
            }

            if (ModelState.IsValid)
            {
                _context.Add(producto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(producto);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var producto = await _context.Productos.FindAsync(id);
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
                try
                {
                    if (imagenArchivo != null && imagenArchivo.Length > 0)
                    {
                        producto.ImagenUrl = await GuardarImagen(imagenArchivo);
                    }
                    _context.Update(producto);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductoExists(producto.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(producto);
        }

        // --- MÉTODOS PARA ELIMINACIÓN Y PAPELERA (SOLUCIONA EL 404) ---
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var producto = await _context.Productos.FirstOrDefaultAsync(m => m.Id == id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Papelera()
        {
            // Aquí puedes implementar Soft Delete en Productos si gustas, por ahora mandamos a Index
            return RedirectToAction(nameof(Index));
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

        private bool ProductoExists(int id) => _context.Productos.Any(e => e.Id == id);
    }
}