using System.ComponentModel.DataAnnotations;

namespace RefaccionariaWeb.Models.ViewModels
{
    public class CrearUsuarioViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }
    }
}