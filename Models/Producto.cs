using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RefaccionariaWeb.Models
{
    public class Producto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string SKU { get; set; } // Código de barras

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        public string? Descripcion { get; set; }
        public string? MarcaPieza { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioVenta { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioCompra { get; set; }

        public int Stock { get; set; }

        // Logística
        public string? Pasillo { get; set; }
        public string? Anaquel { get; set; }

        public string? ImagenUrl { get; set; } // Puede estar vacío si viene de Excel

        public bool EsVisibleEnLinea { get; set; } = true;

        public ICollection<Compatibilidad>? Compatibilidades { get; set; }
    }
}