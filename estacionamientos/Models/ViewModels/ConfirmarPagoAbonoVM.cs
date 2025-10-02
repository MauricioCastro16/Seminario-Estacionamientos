using System.ComponentModel.DataAnnotations;

namespace estacionamientos.ViewModels
{
    public class ConfirmarPagoAbonoVM
    {
        // Datos del abono
        [Required]
        public int PlyID { get; set; }
        
        [Required]
        public int SerID { get; set; }
        
        [Required]
        public int ClasVehID { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int Periodos { get; set; }
        
        [Required]
        public DateTime AboFyhIni { get; set; }
        
        public DateTime? AboFyhFin { get; set; }
        
        [Required]
        public decimal AboMonto { get; set; }

        // Datos del abonado
        [Required]
        [StringLength(15)]
        public string AboDNI { get; set; } = "";
        
        [Required]
        [StringLength(120)]
        public string AboNom { get; set; } = "";

        // Datos de la plaza
        [Required]
        [Range(1, int.MaxValue)]
        public int SelectedPlzNum { get; set; }
        
        public bool SelectedPlzTecho { get; set; }

        // Datos del pago
        [Required]
        public int MepID { get; set; }
        
        [Required]
        public string OpcionPago { get; set; } = ""; // "todo" o "parcial"
        
        [Required]
        [Range(1, int.MaxValue)]
        public int CantidadPeriodosPagar { get; set; }
        
        [Required]
        public decimal MontoPagar { get; set; }

        // Vehículos
        public List<VehiculoVM> Vehiculos { get; set; } = new List<VehiculoVM>();
    }
}
