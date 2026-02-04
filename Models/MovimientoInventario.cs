using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RefaccionariaWeb.Models
{
    public class MovimientoInventario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public virtual Producto? Producto { get; set; }

        [Required]
        public string TipoMovimiento { get; set; } // "ENTRADA", "VENTA", "SCRAP", "CANCELACION"

        [Required]
        public int Cantidad { get; set; } // Positivos para entradas, negativos para salidas

        [Required]
        public DateTime FechaRegistro { get; set; }

        public string? Referencia { get; set; } // Ejemplo: "Pedido #10", "Folio Scrap #5", "Factura Prov X"

        public string? UsuarioId { get; set; }
        public string? NombreUsuario { get; set; }
    }
}