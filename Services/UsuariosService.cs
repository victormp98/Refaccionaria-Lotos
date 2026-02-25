using Microsoft.AspNetCore.Identity;
using RefaccionariaWeb.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // Para ToListAsync
using Microsoft.AspNetCore.Mvc.Rendering; // Para SelectListItem en ObtenerParaEditar

namespace RefaccionariaWeb.Services
{
    public class UsuariosService : IUsuariosService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsuariosService(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<List<UsuarioViewModel>> ObtenerTodos(string tipo, string buscar = null, bool soloBloqueados = false)
        {
            var usuarios = await _userManager.Users.ToListAsync();
            var usuariosViewModel = new List<UsuarioViewModel>();

            foreach (var user in usuarios)
            {
                var estaBloqueado = await _userManager.IsLockedOutAsync(user);

                // Lógica de filtro para activos vs. bloqueados (papelera)
                if (soloBloqueados != estaBloqueado) continue;

                var roles = await _userManager.GetRolesAsync(user);
                string rolPrincipal = roles.FirstOrDefault() ?? "Sin Rol";

                bool cumpleFiltroTipo = false;
                if (tipo == "personal")
                {
                    cumpleFiltroTipo = rolPrincipal != "Cliente";
                }
                else if (tipo == "clientes")
                {
                    cumpleFiltroTipo = rolPrincipal == "Cliente";
                }
                else // Si tipo no es "personal" ni "clientes", mostrar todos los activos o bloqueados según soloBloqueados
                {
                    cumpleFiltroTipo = true;
                }

                if (!cumpleFiltroTipo) continue;

                // Aplicar filtro de búsqueda por Email o UserName
                bool cumpleFiltroBuscar = true;
                if (!string.IsNullOrEmpty(buscar))
                {
                    string termino = buscar.ToLower();
                    cumpleFiltroBuscar = (user.Email != null && user.Email.ToLower().Contains(termino)) ||
                                         (user.UserName != null && user.UserName.ToLower().Contains(termino));
                }

                if (cumpleFiltroBuscar)
                {
                    usuariosViewModel.Add(new UsuarioViewModel
                    {
                        Id = user.Id,
                        Email = user.Email,
                        UserName = user.UserName,
                        Rol = rolPrincipal,
                        EstaBloqueado = estaBloqueado
                    });
                }
            }

            return usuariosViewModel;
        }

        public async Task<EditarUsuarioViewModel?> ObtenerParaEditar(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null) return null;

            var rolesUsuario = await _userManager.GetRolesAsync(usuario);

            return new EditarUsuarioViewModel
            {
                Id = usuario.Id,
                Email = usuario.Email,
                // UserName = usuario.UserName,  <-- Borrada
                Telefono = usuario.PhoneNumber,
                RolSeleccionado = rolesUsuario.FirstOrDefault(),
                EstaBloqueado = await _userManager.IsLockedOutAsync(usuario),
                ListaRoles = await _roleManager.Roles.Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Name,
                    Selected = rolesUsuario.Contains(r.Name)
                }).ToListAsync()
            };
        }

        public async Task<IdentityResult> Crear(CrearUsuarioViewModel model)
        {
            var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Por defecto, asignar Mostrador si no se especifica (puedes ajustar esto)
                await _userManager.AddToRoleAsync(user, "Mostrador");
            }
            return result;
        }

        public async Task<bool> Editar(EditarUsuarioViewModel model)
        {
            var usuario = await _userManager.FindByIdAsync(model.Id);
            if (usuario == null) return false;

            usuario.Email = model.Email;
            usuario.UserName = model.Email;
            usuario.PhoneNumber = model.Telefono;

            var result = await _userManager.UpdateAsync(usuario);
            if (!result.Succeeded) return false;

            if (!string.IsNullOrEmpty(model.RolSeleccionado))
            {
                var rolesActuales = await _userManager.GetRolesAsync(usuario);
                var resultRemove = await _userManager.RemoveFromRolesAsync(usuario, rolesActuales);
                if (!resultRemove.Succeeded) return false;

                var resultAdd = await _userManager.AddToRoleAsync(usuario, model.RolSeleccionado);
                if (!resultAdd.Succeeded) return false;
            }

            bool lockoutSuccess;
            if (model.EstaBloqueado)
                lockoutSuccess = await Bloquear(model.Id);
            else
                lockoutSuccess = await Desbloquear(model.Id);

            return lockoutSuccess;
        }

        public async Task<bool> Bloquear(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null) return false;

            var result = await _userManager.SetLockoutEndDateAsync(usuario, DateTimeOffset.UtcNow.AddYears(100));
            if (!result.Succeeded) return false;

            var resetResult = await _userManager.ResetAccessFailedCountAsync(usuario);
            return resetResult.Succeeded;
        }

        public async Task<bool> Desbloquear(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null) return false;

            var result = await _userManager.SetLockoutEndDateAsync(usuario, null);
            if (!result.Succeeded) return false;

            var resetResult = await _userManager.ResetAccessFailedCountAsync(usuario);
            return resetResult.Succeeded;
        }

        public async Task<List<string>> ObtenerRoles()
        {
            return await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        }
    }
}
