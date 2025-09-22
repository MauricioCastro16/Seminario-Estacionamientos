using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models.ViewModels
{
    public class ConfigurarPlazaVM
    {
        [Required(ErrorMessage = "Ingrese la cantidad de plazas.")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "Seleccione si tiene techo.")]
        public bool? PlzTecho { get; set; }

        public decimal? PlzAlt { get; set; }

        [Required(ErrorMessage = "Ingrese la planta.")]
        [Range(0, int.MaxValue, ErrorMessage = "La planta debe ser 0 o superior.")]
        public int Piso { get; set; }

        [Required(ErrorMessage = "Seleccione al menos un vehículo.")]
        public List<int> ClasVehID { get; set; } = new();

        public int PlyID { get; set; }
    }
}
