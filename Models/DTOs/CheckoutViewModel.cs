using System.ComponentModel.DataAnnotations;

namespace RefaccionariaWeb.Models.DTOs
{
    public class CheckoutViewModel
    {
        [Required]
        [StringLength(150)]
        public string NombreReceptor { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string DireccionEnvio { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CiudadEnvio { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string EstadoEnvio { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string CodigoPostalEnvio { get; set; } = string.Empty;

        public bool RequiereFactura { get; set; }

        [StringLength(13)]
        public string? Rfc { get; set; }

        [StringLength(250)]
        public string? RazonSocial { get; set; }

        public int TipoEntrega { get; set; }
    }
}
