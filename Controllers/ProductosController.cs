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

namespace RefaccionariaWeb.Controllers
{
    // Quitamos [Authorize] general para personalizar por acción
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        // 1. ZONA DE EMPLEADOS (Inventario Interno)
        // =========================================================================

        // GET: Productos (La Tabla tipo Excel)
        // OJO: El Cliente NO entra aquí, él usa el Home/Index
        [Authorize(Roles = "Admin,Almacen,Mostrador")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Productos.Where(p => p.EsVisibleEnLinea == true).ToListAsync());
        }

        // =========================================================================
        // 2. ZONA PÚBLICA / MIXTA (Detalles del Producto)
        // =========================================================================

        // GET: Productos/Details/5
        // AQUI SÍ entra el Cliente, y también los empleados
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos
                .Include(p => p.Compatibilidades)
                .ThenInclude(c => c.Vehiculo)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (producto == null) return NotFound();

            // FILTRO EN MEMORIA: Quitamos de la lista los que tengan Activo == false
            // (Esto asegura que en la vista no aparezcan los de la papelera)
            producto.Compatibilidades = producto.Compatibilidades
                .Where(c => c.Vehiculo != null && c.Vehiculo.Activo == true) // <--- ESTA ES LA CLAVE
                .ToList();

            return View(producto);
        }

        // =========================================================================
        // 3. ZONA ADMINISTRATIVA (Crear, Editar, Borrar)
        // =========================================================================

        // GET: Productos/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,SKU,Nombre,Descripcion,MarcaPieza,PrecioVenta,PrecioCompra,Stock,Pasillo,Anaquel,EsVisibleEnLinea")] Producto producto, IFormFile? imagenArchivo)
        {
            if (imagenArchivo != null && imagenArchivo.Length > 0)
            {
                var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(imagenArchivo.FileName);
                var rutaGuardado = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\imagenes", nombreArchivo);
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

        // GET: Productos/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

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
                        var rutaGuardado = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\imagenes", nombreArchivo);
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

        // GET: Productos/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var producto = await _context.Productos.FirstOrDefaultAsync(m => m.Id == id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        // POST: Productos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                producto.EsVisibleEnLinea = false; // Soft Delete
                _context.Update(producto);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Productos/Papelera
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Papelera()
        {
            var productosBorrados = await _context.Productos.Where(p => p.EsVisibleEnLinea == false).ToListAsync();
            return View(productosBorrados);
        }

        // GET: Productos/Restaurar
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restaurar(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                producto.EsVisibleEnLinea = true;
                _context.Update(producto);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Papelera));
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.Id == id);
        }
    }
}