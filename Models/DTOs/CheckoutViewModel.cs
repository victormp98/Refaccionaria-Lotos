using System.ComponentModel.DataAnnotations;

namespace RefaccionariaWeb.Models.DTOs
{
    public class CheckoutViewModel
    {
        [Required]
        [StringLength(150)]
        public string NombreReceptor { get; set; }

        [Required]
        [StringLength(200)]
        public string DireccionEnvio { get; set; }

        [Required]
        [StringLength(100)]
        public string CiudadEnvio { get; set; }

        [Required]
        [StringLength(100)]
        public string EstadoEnvio { get; set; }

        [Required]
        [StringLength(10)]
        public string CodigoPostalEnvio { get; set; }

        public bool RequiereFactura { get; set; }

        [StringLength(13)]
        public string? Rfc { get; set; }

        [StringLength(250)]
        public string? RazonSocial { get; set; }

        public int TipoEntrega { get; set; }
    }
}
