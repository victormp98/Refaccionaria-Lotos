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
        [Required] // Mantenemos Required, se usará el ID del "Público General"
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
        [StringLength(200)]
        public string? DireccionEnvio { get; set; } // Ahora nullable

        [StringLength(100)]
        public string? CiudadEnvio { get; set; } // Ahora nullable

        [StringLength(100)]
        public string? EstadoEnvio { get; set; } // Ahora nullable

        [StringLength(10)]
        public string? CodigoPostalEnvio { get; set; } // Ahora nullable

        [StringLength(100)]
        public string? PaisEnvio { get; set; } = "México";

        [StringLength(150)]
        public string? NombreReceptor { get; set; } // Ahora nullable

        // === INFORMACIÓN DE FACTURACIÓN (SNAPSHOT) ===
        public bool RequiereFactura { get; set; }

        [StringLength(13)] // RFC tiene 12 o 13 caracteres (física o moral)
        public string? Rfc { get; set; }

        [StringLength(250)]
        public string? RazonSocial { get; set; }

        // === NUEVOS CAMPOS DE LOGÍSTICA Y CONTROL ===
        // 0 = Envío domicilio, 1 = Retiro en sucursal, 2 = Venta Mostrador
        public int TipoEntrega { get; set; }

        [StringLength(100)]
        public string? Paqueteria { get; set; }

        [StringLength(100)]
        public string? NumeroGuia { get; set; }

        public DateTime? FechaEnvio { get; set; }
        
        public int? CorteCajaId { get; set; }
        [ForeignKey("CorteCajaId")]
        public virtual CorteCaja? CorteCaja { get; set; } // Ahora nullable

        public string? EmpleadoId { get; set; } // Nuevo campo para el empleado
        [ForeignKey("EmpleadoId")]
        public virtual IdentityUser? Empleado { get; set; } // Relación con el empleado

        // === RELACIÓN CON LOS DETALLES DEL PEDIDO ===
        public virtual ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();
    }
}
