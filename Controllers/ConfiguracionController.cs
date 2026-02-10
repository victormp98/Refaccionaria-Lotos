using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using RefaccionariaWeb.Models;
using System.Threading.Tasks;

namespace RefaccionariaWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ConfiguracionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConfiguracionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Edit()
        {
            var config = await _context.SucursalConfigs.FindAsync(1);
            if (config == null) return NotFound();
            return View(config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SucursalConfig config)
        {
            if (ModelState.IsValid)
            {
                config.Id = 1;
                _context.Update(config);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "¡Configuración de la tienda actualizada!";
                return RedirectToAction(nameof(Edit));
            }
            return View(config);
        }
    }
}