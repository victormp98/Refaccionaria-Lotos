using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using RefaccionariaWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RefaccionariaWeb.Controllers
{
    // 1. QUITAMOS EL CANDADO GENERAL (Para permitir acceso selectivo)
    public class CompatibilidadesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CompatibilidadesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        // ZONA PÚBLICA PARA EMPLEADOS (Ver Compatibilidades)
        // =========================================================================

        // GET: Compatibilidades
        [Authorize(Roles = "Admin,Almacen,Mostrador")] // <--- TODOS LOS EMPLEADOS
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Compatibilidades
                .Include(c => c.Producto)
                .Include(c => c.Vehiculo)
                // FILTRO MÁGICO: Solo mostrar si AMBOS padres están activos/visibles
                .Where(c => c.Vehiculo.Activo == true && c.Producto.EsVisibleEnLinea == true);

            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Compatibilidades/Details/5
        [Authorize(Roles = "Admin,Almacen,Mostrador")] // <--- TODOS LOS EMPLEADOS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var compatibilidad = await _context.Compatibilidades
                .Include(c => c.Producto)
                .Include(c => c.Vehiculo)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (compatibilidad == null) return NotFound();

            return View(compatibilidad);
        }

        // =========================================================================
        // ZONA RESTRINGIDA (Solo el Admin conecta cables)
        // =========================================================================

        // GET: Compatibilidades/Create
        [Authorize(Roles = "Admin")] // <--- SOLO ADMIN
        public IActionResult Create()
        {
            // Nota: Aquí podrías filtrar también para que solo salgan productos/autos activos
            // Pero como es Admin, le dejamos ver todo por si acaso.
            ViewData["ProductoId"] = new SelectList(_context.Productos, "Id", "Nombre");
            ViewData["VehiculoId"] = new SelectList(_context.Vehiculos, "Id", "Marca");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ProductoId,VehiculoId")] Compatibilidad compatibilidad)
        {
            // VALIDACIÓN: ¿Ya existe esta combinación en la BD?
            bool existe = _context.Compatibilidades.Any(c =>
                c.ProductoId == compatibilidad.ProductoId &&
                c.VehiculoId == compatibilidad.VehiculoId);

            if (existe)
            {
                // Agregamos error al modelo para que salga en rojo en la vista
                ModelState.AddModelError("", "¡Este vehículo ya está asignado a este producto!");
            }

            if (ModelState.IsValid)
            {
                _context.Add(compatibilidad);
                await _context.SaveChangesAsync();
                // Regresamos a la lista filtrada por producto
                return RedirectToAction(nameof(Index), new { productoId = compatibilidad.ProductoId });
            }

            // Si falló, recargamos los datos necesarios para la vista
            ViewData["ProductoId"] = new SelectList(_context.Productos, "Id", "Nombre", compatibilidad.ProductoId);
            ViewData["VehiculoId"] = new SelectList(_context.Vehiculos.Where(v => v.Activo), "Id", "Modelo", compatibilidad.VehiculoId);
            return View(compatibilidad);
        }
        // GET: Compatibilidades/Edit/5
        [Authorize(Roles = "Admin")] // <--- SOLO ADMIN
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var compatibilidad = await _context.Compatibilidades.FindAsync(id);
            if (compatibilidad == null) return NotFound();
            ViewData["ProductoId"] = new SelectList(_context.Productos, "Id", "Nombre", compatibilidad.ProductoId);
            ViewData["VehiculoId"] = new SelectList(_context.Vehiculos, "Id", "Marca", compatibilidad.VehiculoId);
            return View(compatibilidad);
        }

        // POST: Compatibilidades/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // <--- SOLO ADMIN
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProductoId,VehiculoId,NotaTecnica")] Compatibilidad compatibilidad)
        {
            if (id != compatibilidad.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(compatibilidad);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CompatibilidadExists(compatibilidad.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProductoId"] = new SelectList(_context.Productos, "Id", "Nombre", compatibilidad.ProductoId);
            ViewData["VehiculoId"] = new SelectList(_context.Vehiculos, "Id", "Marca", compatibilidad.VehiculoId);
            return View(compatibilidad);
        }

        // GET: Compatibilidades/Delete/5
        [Authorize(Roles = "Admin")] // <--- SOLO ADMIN
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var compatibilidad = await _context.Compatibilidades
                .Include(c => c.Producto)
                .Include(c => c.Vehiculo)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (compatibilidad == null) return NotFound();

            return View(compatibilidad);
        }

        // POST: Compatibilidades/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // <--- SOLO ADMIN
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var compatibilidad = await _context.Compatibilidades.FindAsync(id);
            if (compatibilidad != null)
            {
                _context.Compatibilidades.Remove(compatibilidad);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CompatibilidadExists(int id)
        {
            return _context.Compatibilidades.Any(e => e.Id == id);
        }
    }
}