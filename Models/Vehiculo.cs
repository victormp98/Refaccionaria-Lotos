using System.ComponentModel.DataAnnotations;

namespace RefaccionariaWeb.Models
{
    public class Vehiculo
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La marca es obligatoria")]
        [StringLength(50)]
        public string Marca { get; set; } // Ej: Nissan

        [Required(ErrorMessage = "El modelo es obligatorio")]
        [StringLength(50)]
        public string Modelo { get; set; } // Ej: Tsuru

        public int AnioInicio { get; set; } // Ej: 1992
        public int AnioFin { get; set; }    // Ej: 2017

        [StringLength(50)]
        public string? Motor { get; set; } // Ej: 1.6L
        public bool Activo { get; set; } = true;
        // Relación: Un vehículo tiene muchas piezas compatibles
        public ICollection<Compatibilidad>? Compatibilidades { get; set; }
    }

}