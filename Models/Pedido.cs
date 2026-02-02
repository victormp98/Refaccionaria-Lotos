using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity; // For IdentityUser
using RefaccionariaWeb.Models.Enums; // For PedidoStatus

namespace RefaccionariaWeb.Models
{
    // CORRECCIÓN AQUÍ: Forzamos el nombre en minúsculas para coincidir con Linux
    [Table("pedidos")]
    public class Pedido
    {
        [Key]
        public int Id { get; set; }

        // === RELACIÓN CON EL CLIENTE ===
        [Required]
        public string ClienteId { get; set; } // FK a AspNetUsers (IdentityUser)
        [ForeignKey("ClienteId")]
        public virtual IdentityUser Cliente { get; set; }

        // === INFORMACIÓN GENERAL DEL PEDIDO ===
        [Required]
        public DateTime FechaPedido { get; set; } = DateTime.Now;

        [Required]
        public PedidoStatus Status { get; set; } = PedidoStatus.PendienteDePago;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPedido { get; set; }

        // === TRAZABILIDAD DE ENVÍO (SNAPSHOT DE DIRECCIÓN) ===
        // Campos para guardar la dirección EXACTA del pedido en el momento de la compra
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

        [StringLength(100)]
        public string PaisEnvio { get; set; } = "México"; // Asunción por defecto, se puede cambiar

        [Required]
        [StringLength(150)]
        public string NombreReceptor { get; set; } // Nombre de la persona que recibe el pedido

        // === INFORMACIÓN DE FACTURACIÓN (SNAPSHOT) ===
        public bool RequiereFactura { get; set; }

        [StringLength(13)] // RFC tiene 12 o 13 caracteres (física o moral)
        public string? Rfc { get; set; }

        [StringLength(250)]
        public string? RazonSocial { get; set; }

        // === RELACIÓN CON LOS DETALLES DEL PEDIDO ===
        public virtual ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();
    }
}