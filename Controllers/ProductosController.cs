using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using RefaccionariaWeb.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
// Agregamos esta referencia para manejar las rutas correctamente
using Microsoft.AspNetCore.Hosting;

namespace RefaccionariaWeb.Controllers
{
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment; // <--- Agregamos esto

        public ProductosController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment; // <--- Y esto
        }

        // ... (Index, Details y otros métodos se quedan igual) ...

        // POST: Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,SKU,Nombre,Descripcion,MarcaPieza,PrecioVenta,PrecioCompra,Stock,Pasillo,Anaquel,EsVisibleEnLinea")] Producto producto, IFormFile? imagenArchivo)
        {
            if (imagenArchivo != null && imagenArchivo.Length > 0)
            {
                // 1. Nombre de archivo único
                var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(imagenArchivo.FileName);

                // 2. Usamos Path.Combine y WebRootPath para que sea multiplataforma
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "imagenes");

                // SEGURIDAD: Si la carpeta no existe, la creamos
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

        // POST: Productos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SKU,Nombre,Descripcion,MarcaPieza,PrecioVenta,PrecioCompra,Stock,Pasillo,Anaquel,ImagenUrl,EsVisibleEnLinea")] Producto producto, IFormFile? imagenArchivo)
        {
            if (id != producto.Id) return NotFound();

            ModelState.Remove("imagenArchivo");

            if (ModelState.IsValid)
            {
                try
                {
                    if (imagenArchivo != null && imagenArchivo.Length > 0)
                    {
                        var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(imagenArchivo.FileName);

                        // Usamos la misma lógica multiplataforma que en el Create
                        string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "imagenes");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        string rutaGuardado = Path.Combine(uploadsFolder, nombreArchivo);

                        using (var stream = new FileStream(rutaGuardado, FileMode.Create))
                        {
                            await imagenArchivo.CopyToAsync(stream);
                        }
                        producto.ImagenUrl = "/imagenes/" + nombreArchivo;
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

        // ... (El resto de métodos se quedan igual) ...

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.Id == id);
        }
    }
}