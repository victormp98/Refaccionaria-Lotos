using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace RefaccionariaWeb.Models.ViewModels
{
    public class EditarUsuarioViewModel
    {
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; }

        [Display(Name = "Número de Teléfono")]
        public string? Telefono { get; set; }

        // Aquí guardaremos el rol que seleccione el Admin
        [Display(Name = "Puesto / Rol")]
        public string RolSeleccionado { get; set; }

        // Esta es la lista que llenará el Dropdown (Combo box)
        public List<SelectListItem>? ListaRoles { get; set; }

        public bool EstaBloqueado { get; set; }
    }
}