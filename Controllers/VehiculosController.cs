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
    // 1. QUITAMOS EL CANDADO GENERAL (Para abrir Index y Details)
    public class VehiculosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VehiculosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        // ZONA PÚBLICA PARA EMPLEADOS (Ver Catálogo de Autos)
        // =========================================================================

        // GET: Vehiculos
        [Authorize(Roles = "Admin,Almacen,Mostrador")] // <--- TODOS LOS EMPLEADOS
        public async Task<IActionResult> Index()
        {
            // Solo mostramos los ACTIVOS
            return View(await _context.Vehiculos.Where(v => v.Activo == true).ToListAsync());
        }

        // GET: Vehiculos/Details/5
        [Authorize(Roles = "Admin,Almacen,Mostrador")] // <--- TODOS LOS EMPLEADOS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var vehiculo = await _context.Vehiculos.FirstOrDefaultAsync(m => m.Id == id);
            if (vehiculo == null) return NotFound();

            return View(vehiculo);
        }

        // =========================================================================
        // ZONA RESTRINGIDA (Solo el Jefe/Admin gestiona los autos)
        // =========================================================================

        // GET: Vehiculos/Create
        [Authorize(Roles = "Admin")] // <--- SOLO ADMIN
        public IActionResult Create()
        {
            return View();
        }

        // POST: Vehiculos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // <--- SOLO ADMIN
        public async Task<IActionResult> Create([Bind("Id,Marca,Modelo,AnioInicio,AnioFin,Motor,Activo")] Vehiculo vehiculo)
        {
            if (ModelState.IsValid)
            {
                vehiculo.Activo = true;
                _context.Add(vehiculo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(vehiculo);
        }

        // GET: Vehiculos/Edit/5
        [Authorize(Roles = "Admin")] // <--- SOLO ADMIN
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var vehiculo = await _context.Vehiculos.FindAsync(id);
            if (vehiculo == null) return NotFound();
            return View(vehiculo);
        }

        // POST: Vehiculos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // <--- SOLO ADMIN
        public async Task<IActionResult> Edit(int id, [Bind("Id,Marca,Modelo,AnioInicio,AnioFin,Motor,Activo")] Vehiculo vehiculo)
        {
            if (id != vehiculo.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vehiculo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VehiculoExists(vehiculo.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(vehiculo);
        }

        // GET: Vehiculos/Delete/5
        [Authorize(Roles = "Admin")] // <--- SOLO ADMIN
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var vehiculo = await _context.Vehiculos.FirstOrDefaultAsync(m => m.Id == id);
            if (vehiculo == null) return NotFound();

            return View(vehiculo);
        }

        // POST: Vehiculos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // <--- SOLO ADMIN
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vehiculo = await _context.Vehiculos.FindAsync(id);
            if (vehiculo != null)
            {
                vehiculo.Activo = false; // Soft Delete
                _context.Update(vehiculo);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------------------------------------------------
        // MÉTODOS DE LA PAPELERA (SOLO ADMIN)
        // ---------------------------------------------------

        // GET: Vehiculos/Papelera
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Papelera()
        {
            return View(await _context.Vehiculos.Where(v => v.Activo == false).ToListAsync());
        }

        // GET: Vehiculos/Restaurar/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restaurar(int? id)
        {
            if (id == null) return NotFound();

            var vehiculo = await _context.Vehiculos.FindAsync(id);
            if (vehiculo != null)
            {
                vehiculo.Activo = true;
                _context.Update(vehiculo);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Papelera));
        }

        private bool VehiculoExists(int id)
        {
            return _context.Vehiculos.Any(e => e.Id == id);
        }
    }
}