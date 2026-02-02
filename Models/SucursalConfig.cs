using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RefaccionariaWeb.Models
{
    [Table("SucursalConfig")]
    public class SucursalConfig
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; } = 1;

        [Required(ErrorMessage = "El nombre de la tienda es obligatorio")]
        public string NombreTienda { get; set; }

        [Required(ErrorMessage = "La dirección es obligatoria")]
        public string Direccion { get; set; }

        [Required(ErrorMessage = "La ciudad es obligatoria")]
        public string Ciudad { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        public string Estado { get; set; }

        [Required(ErrorMessage = "El código postal es obligatorio")]
        public string CP { get; set; }

        public string Telefono { get; set; }
    }
}