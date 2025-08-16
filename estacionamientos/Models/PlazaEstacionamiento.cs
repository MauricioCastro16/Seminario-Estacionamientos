using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class PlazaEstacionamiento
    {
        // PK compuesta
        public int PlyID { get; set; }      // FK -> PlayaEstacionamiento
        public int PlzNum { get; set; }     // Número de plaza dentro de la playa

        public bool PlzOcupada { get; set; }   // estado actual (opcional mantenerlo por performance)
        public bool PlzTecho { get; set; }
        public decimal? PlzAlt { get; set; }    // altura máx en metros (ajustá tipo si querés)
        public bool PlzHab { get; set; } = true; // habilitada

        // Navs
        public PlayaEstacionamiento Playa { get; set; } = default!;
        public ICollection<Ocupacion> Ocupaciones { get; set; } = new List<Ocupacion>();
    }
}
