using System.ComponentModel.DataAnnotations.Schema;

namespace RefaccionariaWeb.Models
{
    public class Compatibilidad
    {
        public int Id { get; set; }

        public int ProductoId { get; set; }
        [ForeignKey("ProductoId")]
        public Producto? Producto { get; set; }

        public int VehiculoId { get; set; }
        [ForeignKey("VehiculoId")]
        public Vehiculo? Vehiculo { get; set; }

        public string? NotaTecnica { get; set; }
    }
}