using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models.ViewModels
{
    public class CobroEgresoVM
    {
        // Datos de la ocupación
        public int PlyID { get; set; }
        public int PlzNum { get; set; }
        public string VehPtnt { get; set; } = "";
        public DateTime OcufFyhIni { get; set; }
        public DateTime OcufFyhFin { get; set; }
        
        // Datos del vehículo
        public int ClasVehID { get; set; }
        public string ClasVehTipo { get; set; } = "";
        
        // Datos de la playa
        public string PlayaNombre { get; set; } = "";
        
        // Cálculo del tiempo
        public TimeSpan TiempoOcupacion { get; set; }
        public int HorasOcupacion { get; set; }
        public int MinutosOcupacion { get; set; }
        
        // Servicios aplicables
        public List<ServicioCobroVM> ServiciosAplicables { get; set; } = new List<ServicioCobroVM>();
        
        // Total a cobrar
        public decimal TotalCobro { get; set; }
        
        // Método de pago seleccionado
        [Required(ErrorMessage = "Debe seleccionar un método de pago")]
        public int MepID { get; set; }
        
        // Métodos de pago disponibles
        public List<MetodoPagoVM> MetodosPagoDisponibles { get; set; } = new List<MetodoPagoVM>();
    }
    
    public class ServicioCobroVM
    {
        public int SerID { get; set; }
        public string SerNom { get; set; } = "";
        public string SerTipo { get; set; } = "";
        public decimal TarifaVigente { get; set; }
        public int Cantidad { get; set; }
        public decimal Subtotal { get; set; }
        public bool EsEstacionamiento { get; set; }
    }
    
    public class MetodoPagoVM
    {
        public int MepID { get; set; }
        public string MepNom { get; set; } = "";
        public string? MepDesc { get; set; }
    }
}

