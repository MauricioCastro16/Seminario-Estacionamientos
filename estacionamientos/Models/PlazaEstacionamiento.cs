using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class PlazaEstacionamiento
    {
        // PK compuesta
        public int PlyID { get; set; }      // FK -> PlayaEstacionamiento
        public int PlzNum { get; set; }     // Número de plaza dentro de la playa

        public bool PlzOcupada { get; set; } = false;  // estado actual (opcional mantenerlo por performance)
        public bool PlzTecho { get; set; }
        public decimal? PlzAlt { get; set; }    // altura máx en metros (ajustá tipo si querés)
        public bool PlzHab { get; set; } = true; // habilitada
        public string? PlzNombre { get; set; } // nombre o código identificatorio (opcional)

        public int? Piso { get; set; }
        //public int ClasVehID { get; set; }
        //public ClasificacionVehiculo Clasificacion { get; set; } = default!;
        public ICollection<PlazaClasificacion> Clasificaciones { get; set; } = new List<PlazaClasificacion>();
        // Navs
        public PlayaEstacionamiento Playa { get; set; } = default!;
        public ICollection<Ocupacion> Ocupaciones { get; set; } = new List<Ocupacion>();
        public ICollection<Abono> Abonos { get; set; } = new List<Abono>();
    }
}
