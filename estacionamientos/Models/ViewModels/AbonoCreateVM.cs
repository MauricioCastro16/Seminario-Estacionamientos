using System.ComponentModel.DataAnnotations;
using estacionamientos.Helpers;

namespace estacionamientos.ViewModels
{
    public class AbonoCreateVM
    {
        // Datos de Abono
        [Required(ErrorMessage = ErrorMessages.CampoObligatorio)]
        public int PlyID { get; set; }
        
        [Required(ErrorMessage = ErrorMessages.CampoObligatorio)]
        public int PlzNum { get; set; }
        
        [Required(ErrorMessage = ErrorMessages.SeleccioneClasificacionVehiculo)]
        public int ClasVehID { get; set; }
        
        public int? SerID { get; set; } // Tipo de abono (servicio)
        
        [Required(ErrorMessage = ErrorMessages.CampoObligatorio)]
        [Range(1, int.MaxValue, ErrorMessage = ErrorMessages.CantidadMayorCero)]
        public int Periodos { get; set; } = 1; // Cantidad de períodos del servicio
        
        [Required(ErrorMessage = ErrorMessages.CampoObligatorio)]
        public DateTime AboFyhIni { get; set; } = DateTime.UtcNow;
        
        public DateTime? AboFyhFin { get; set; }
        public decimal? AboMonto { get; set; }

        // Datos de abonado
        [Required(ErrorMessage = ErrorMessages.CampoObligatorio)]
        [StringLength(11, MinimumLength = 7, ErrorMessage = ErrorMessages.DNIInvalido)]
        public string AboDNI { get; set; } = "";
        
        [Required(ErrorMessage = ErrorMessages.CampoObligatorio)]
        [StringLength(120, ErrorMessage = ErrorMessages.NombreExcedeCaracteres)]
        public string AboNom { get; set; } = "";

        // Pago
        [Required(ErrorMessage = ErrorMessages.SeleccioneMetodoPago)]
        public int MepID { get; set; }

        // Vehículos (pueden ser varios)
        [Required(ErrorMessage = "Debe agregar al menos un vehículo para el abono.")]
        [MinLength(1, ErrorMessage = "Debe agregar al menos un vehículo para el abono.")]
        public List<VehiculoVM> Vehiculos { get; set; } = new();

        // Preferencias de plaza
        public bool? PlzTecho { get; set; }
        public int? Piso { get; set; }
        
        // Plaza seleccionada
        public int? SelectedPlzNum { get; set; }
        public bool? SelectedPlzTecho { get; set; }
    }

    public class VehiculoVM
    {
        [Required(ErrorMessage = ErrorMessages.CampoObligatorio)]
        [StringLength(10, MinimumLength = 6, ErrorMessage = ErrorMessages.PatenteInvalida)]
        public string VehPtnt { get; set; } = "";
        
        [Required(ErrorMessage = ErrorMessages.CampoObligatorio)]
        public int ClasVehID { get; set; }
    }
}
