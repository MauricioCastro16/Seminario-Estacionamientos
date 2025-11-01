using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class ServicioExtraRealizado
    {
        // PK compuesta
        public int PlyID { get; set; }
        public int SerID { get; set; }
        public string VehPtnt { get; set; } = "";
        [Required]
        public DateTime ServExFyHIni { get; set; }

        public DateTime? ServExFyHFin { get; set; }
        [StringLength(200)]
        public string? ServExComp { get; set; } // comentario

        // Pago (opcional) — FK compuesta a Pago
        public int? PagNum { get; set; }

    // Navs (nullable to avoid MVC treating them as required on POST)
    public ServicioProveido? ServicioProveido { get; set; }
    public Vehiculo? Vehiculo { get; set; }
        public Pago? Pago { get; set; }

        [StringLength(20)]
        public string ServExEstado { get; set; } = "Pendiente";

    }
}
