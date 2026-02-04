using System.ComponentModel.DataAnnotations;

namespace RefaccionariaWeb.Models
{
    public class Scrap
    {
        public int Id { get; set; }

        [Required]
        public int ProductoId { get; set; }
        public virtual Producto? Producto { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "Debes escribir el motivo del scrap")]
        public string Motivo { get; set; } // Texto libre del almacenista

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Auditoría: Quién lo hizo
        public string? UsuarioId { get; set; }
        public string? NombreUsuario { get; set; }
    }
}