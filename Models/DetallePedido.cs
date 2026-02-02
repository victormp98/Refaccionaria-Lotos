using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RefaccionariaWeb.Models
{
    // CORRECCIÓN AQUÍ: Forzamos el nombre en minúsculas para coincidir con Linux
    [Table("detallespedido")]
    public class DetallePedido
    {
        [Key]
        public int Id { get; set; }

        // === RELACIÓN CON EL PEDIDO ===
        [Required]
        public int PedidoId { get; set; } // FK a Pedido
        [ForeignKey("PedidoId")]
        public virtual Pedido Pedido { get; set; }

        // === RELACIÓN CON EL PRODUCTO ===
        [Required]
        public int ProductoId { get; set; } // FK a Producto
        [ForeignKey("ProductoId")]
        public virtual Producto Producto { get; set; }

        // === TRAZABILIDAD DE PRECIO (SNAPSHOT DE COSTO) ===
        [Required]
        public int Cantidad { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioUnitario { get; set; } // Precio del producto en el momento de la compra
    }
}