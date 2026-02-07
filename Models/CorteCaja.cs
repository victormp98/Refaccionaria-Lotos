using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace RefaccionariaWeb.Models
{
    // Nombre en minúsculas para compatibilidad Linux/MySQL
    [Table("cortescaja")]
    public class CorteCaja
    {
        [Key]
        public int Id { get; set; }

        // === QUIÉN ABRIÓ LA CAJA ===
        [Required]
        public string UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual IdentityUser Usuario { get; set; }

        // === TIEMPOS ===
        [Required]
        public DateTime FechaApertura { get; set; } = DateTime.Now;

        public DateTime? FechaCierre { get; set; } // Si es NULL, la caja sigue abierta

        // === DINERO (Decimales exactos) ===

        // 1. Fondo Inicial (Lo que le das en la mano al cajero)
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoInicial { get; set; }

        // 2. Ventas Calculadas (Se llenan automático al cerrar)
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalVentasEfectivo { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalVentasTarjeta { get; set; } = 0;

        // 3. Lo que cuenta el cajero físicamente
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoDeclarado { get; set; } = 0;

        // 4. Resultado (Sobrante o Faltante)
        [Column(TypeName = "decimal(18,2)")]
        public decimal Diferencia { get; set; } = 0;
    }
}