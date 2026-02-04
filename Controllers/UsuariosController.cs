using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Models.ViewModels;
using System.Threading;

namespace RefaccionariaWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsuariosController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;

        public UsuariosController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserStore<IdentityUser> userStore)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
        }

        public async Task<IActionResult> Index(string tipo)
        {
            var usuarios = await _userManager.Users.ToListAsync();
            var modelo = new List<EditarUsuarioViewModel>();

            foreach (var user in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(user);
                modelo.Add(new EditarUsuarioViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    RolSeleccionado = roles.FirstOrDefault()
                });
            }

            ViewData["TipoActual"] = tipo;

            if (tipo == "personal")
            {
                modelo = modelo.Where(u => u.RolSeleccionado != "Cliente").ToList();
                ViewData["Subtitulo"] = "Personal del Sistema";
            }
            else if (tipo == "clientes")
            {
                modelo = modelo.Where(u => u.RolSeleccionado == "Cliente").ToList();
                ViewData["Subtitulo"] = "Lista de Clientes";
            }

            return View(modelo);
        }

        public async Task<IActionResult> Papelera(string tipo)
        {
            var usuarios = await _userManager.Users.ToListAsync();
            var listaBorrados = new List<EditarUsuarioViewModel>();

            foreach (var usuario in usuarios)
            {
                if (await _userManager.IsLockedOutAsync(usuario))
                {
                    var roles = await _userManager.GetRolesAsync(usuario);
                    listaBorrados.Add(new EditarUsuarioViewModel
                    {
                        Id = usuario.Id,
                        Email = usuario.Email,
                        Telefono = usuario.PhoneNumber,
                        RolSeleccionado = roles.FirstOrDefault() ?? "Sin Rol",
                        EstaBloqueado = true
                    });
                }
            }

            ViewData["TipoActual"] = tipo;
            return View(listaBorrados);
        }

        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearUsuarioViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = Activator.CreateInstance<IdentityUser>();
                await _userStore.SetUserNameAsync(user, model.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, model.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                    await _userManager.AddToRoleAsync(user, "Mostrador");
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(string id, string tipo)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null) return NotFound();

            var rolesUsuario = await _userManager.GetRolesAsync(usuario);
            ViewBag.TipoRetorno = tipo;

            var model = new EditarUsuarioViewModel
            {
                Id = usuario.Id,
                Email = usuario.Email,
                Telefono = usuario.PhoneNumber,
                RolSeleccionado = rolesUsuario.FirstOrDefault(),
                EstaBloqueado = await _userManager.IsLockedOutAsync(usuario),

                ListaRoles = _roleManager.Roles.Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Name
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditarUsuarioViewModel model, string tipo)
        {
            var usuario = await _userManager.FindByIdAsync(model.Id);
            if (usuario == null) return NotFound();

            usuario.Email = model.Email;
            usuario.UserName = model.Email;
            usuario.PhoneNumber = model.Telefono;

            if (model.EstaBloqueado)
            {
                await _userManager.SetLockoutEndDateAsync(usuario, DateTimeOffset.UtcNow.AddYears(100));
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(usuario, null);
            }

            var rolesActuales = await _userManager.GetRolesAsync(usuario);
            if (rolesActuales.Any())
            {
                await _userManager.RemoveFromRolesAsync(usuario, rolesActuales);
            }
            await _userManager.AddToRoleAsync(usuario, model.RolSeleccionado);

            await _userManager.UpdateAsync(usuario);

            if (model.EstaBloqueado)
            {
                return RedirectToAction(nameof(Papelera), new { tipo = tipo });
            }

            return RedirectToAction(nameof(Index), new { tipo = tipo });
        }

        [HttpGet]
        public async Task<IActionResult> Desbloquear(string id, string tipo)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null) return NotFound();

            var result = await _userManager.SetLockoutEndDateAsync(usuario, null);
            if (result.Succeeded)
            {
                await _userManager.ResetAccessFailedCountAsync(usuario);
            }

            // CORRECCIÓN: Retornar a la Papelera manteniendo el tipo
            return RedirectToAction(nameof(Papelera), new { tipo = tipo });
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("El UI requiere una tienda de usuarios con soporte para email.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}