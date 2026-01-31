using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Models.ViewModels;
using System.Threading;

namespace RefaccionariaWeb.Controllers
{
    [Authorize(Roles = "Admin")] // <--- ZONA DE ALTA SEGURIDAD
    public class UsuariosController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        // Inyectamos Stores para poder crear usuarios manualmente
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

        // ==========================================================
        // 1. LISTA DE EMPLEADOS ACTIVOS (Index)
        // ==========================================================
        public async Task<IActionResult> Index()
        {
            var usuarios = await _userManager.Users.ToListAsync();
            var listaUsuariosViewModel = new List<EditarUsuarioViewModel>();

            foreach (var usuario in usuarios)
            {
                // FILTRO: Si está bloqueado, NO lo mostramos aquí (se va a la papelera)
                if (await _userManager.IsLockedOutAsync(usuario))
                {
                    continue;
                }

                var roles = await _userManager.GetRolesAsync(usuario);

                listaUsuariosViewModel.Add(new EditarUsuarioViewModel
                {
                    Id = usuario.Id,
                    Email = usuario.Email,
                    Telefono = usuario.PhoneNumber,
                    RolSeleccionado = roles.FirstOrDefault() ?? "Sin Rol",
                    EstaBloqueado = false // Aquí todos son activos
                });
            }

            return View(listaUsuariosViewModel);
        }

        // ==========================================================
        // 2. PAPELERA DE EMPLEADOS (Bloqueados)
        // ==========================================================
        public async Task<IActionResult> Papelera()
        {
            var usuarios = await _userManager.Users.ToListAsync();
            var listaBorrados = new List<EditarUsuarioViewModel>();

            foreach (var usuario in usuarios)
            {
                // FILTRO INVERSO: Solo mostramos los que SÍ están bloqueados
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
            // Retornamos la vista (necesitarás crear Papelera.cshtml o reutilizar Index)
            return View(listaBorrados);
        }

        // ==========================================================
        // 3. CREAR NUEVO EMPLEADO (Sin cerrar sesión)
        // ==========================================================
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

                // Configuramos email y usuario
                await _userStore.SetUserNameAsync(user, model.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, model.Email, CancellationToken.None);

                // Creamos el usuario con la contraseña
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Auto-confirmamos el email para que no haya problemas de login
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);

                    // Por defecto le damos rol "Mostrador" (luego lo puedes cambiar en Edit)
                    await _userManager.AddToRoleAsync(user, "Mostrador");

                    // ¡IMPORTANTE! Aquí NO llamamos a SignInManager. 
                    // Así tú sigues siendo Admin y el nuevo usuario solo se crea en la BD.

                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // ==========================================================
        // 4. EDICIÓN (Cambiar Rol / Bloquear / Desbloquear)
        // ==========================================================
        public async Task<IActionResult> Edit(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null) return NotFound();

            var rolesUsuario = await _userManager.GetRolesAsync(usuario);

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
        public async Task<IActionResult> Edit(EditarUsuarioViewModel model)
        {
            var usuario = await _userManager.FindByIdAsync(model.Id);
            if (usuario == null) return NotFound();

            usuario.Email = model.Email;
            usuario.UserName = model.Email;
            usuario.PhoneNumber = model.Telefono;

            // Lógica de Bloqueo/Desbloqueo (Papelera)
            if (model.EstaBloqueado)
            {
                // Lo mandamos al año 2999 (Bloqueo efectivo)
                await _userManager.SetLockoutEndDateAsync(usuario, DateTimeOffset.UtcNow.AddYears(100));
            }
            else
            {
                // Le quitamos el candado (Restaurar)
                await _userManager.SetLockoutEndDateAsync(usuario, null);
            }

            var rolesActuales = await _userManager.GetRolesAsync(usuario);
            if (rolesActuales.Any())
            {
                await _userManager.RemoveFromRolesAsync(usuario, rolesActuales);
            }
            await _userManager.AddToRoleAsync(usuario, model.RolSeleccionado);

            await _userManager.UpdateAsync(usuario);

            // Si lo bloqueaste, mándalo a la Papelera, si no, al Index
            if (model.EstaBloqueado)
            {
                return RedirectToAction(nameof(Papelera));
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================================
        // 5. ACCIÓN DIRECTA PARA DESBLOQUEAR (Restaurar desde Papelera)
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> Desbloquear(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null) return NotFound();

            // Usamos la misma lógica que ya tienes en el Edit: 
            // Poner la fecha de bloqueo en null lo activa de inmediato.
            var result = await _userManager.SetLockoutEndDateAsync(usuario, null);

            if (result.Succeeded)
            {
                // Limpiamos los intentos fallidos para que entre limpio
                await _userManager.ResetAccessFailedCountAsync(usuario);
            }

            // Te regresa a la Papelera. Como ya no está bloqueado, 
            // el filtro del método Papelera() lo sacará de la lista automáticamente.
            return RedirectToAction(nameof(Papelera));
        }

        // Método auxiliar requerido por Identity para manejar emails
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