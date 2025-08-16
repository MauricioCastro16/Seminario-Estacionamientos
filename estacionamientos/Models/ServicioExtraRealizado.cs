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

        // Navs
        public ServicioProveido ServicioProveido { get; set; } = default!;
        public Vehiculo Vehiculo { get; set; } = default!;
        public Pago? Pago { get; set; }
    }
}
