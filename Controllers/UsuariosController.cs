using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RefaccionariaWeb.Models.ViewModels;
using RefaccionariaWeb.Services;
using System.Threading.Tasks;

namespace RefaccionariaWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsuariosController : Controller
    {
        private readonly IUsuariosService _usuariosService;

        public UsuariosController(IUsuariosService usuariosService)
        {
            _usuariosService = usuariosService;
        }

        // 1. LISTADO PRINCIPAL
        public async Task<IActionResult> Index(string tipo)
        {
            var modelo = await _usuariosService.ObtenerTodos(tipo);
            ViewData["TipoActual"] = tipo;
            ViewData["Subtitulo"] = tipo == "personal" ? "Personal del Sistema" : "Lista de Clientes";
            return View(modelo);
        }

        // 2. PAPELERA (CONGELADOS)
        public async Task<IActionResult> Papelera(string tipo)
        {
            var modelo = await _usuariosService.ObtenerTodos(tipo, soloBloqueados: true);
            ViewData["TipoActual"] = tipo;
            return View(modelo);
        }

        // 3. CREACIÓN (GET): Recibe el tipo para no perder el origen
        public IActionResult Crear(string tipo)
        {
            ViewBag.TipoActual = tipo;
            return View();
        }

        // 3. CREACIÓN (POST): Redirecciona según el tipo de origen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearUsuarioViewModel model, string tipo)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.TipoActual = tipo;
                return View(model);
            }

            var result = await _usuariosService.Crear(model);
            if (result.Succeeded)
            {
                // Ahora te regresa a la lista correcta (Personal o Clientes)
                return RedirectToAction(nameof(Index), new { tipo = tipo });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            ViewBag.TipoActual = tipo;
            return View(model);
        }

        // 4. EDICIÓN
        public async Task<IActionResult> Edit(string id, string tipo)
        {
            var model = await _usuariosService.ObtenerParaEditar(id);
            if (model == null) return NotFound();

            ViewBag.TipoRetorno = tipo;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditarUsuarioViewModel model, string tipo)
        {
            if (!ModelState.IsValid) return View(model);

            var exito = await _usuariosService.Editar(model);
            if (!exito) return View(model);

            return model.EstaBloqueado
                ? RedirectToAction(nameof(Papelera), new { tipo = tipo })
                : RedirectToAction(nameof(Index), new { tipo = tipo });
        }

        // 5. ACCIÓN RÁPIDA DESBLOQUEAR
        [HttpGet]
        public async Task<IActionResult> Desbloquear(string id, string tipo)
        {
            await _usuariosService.Desbloquear(id);
            return RedirectToAction(nameof(Papelera), new { tipo = tipo });
        }
    }
}