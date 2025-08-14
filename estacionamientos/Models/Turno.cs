using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    // Un turno pertenece a (Playero, Playa) y empieza en TurFyhIni
    public class Turno
    {
        // Parte de la PK y FK compuesta a TrabajaEn
        public int PlyID { get; set; }   // Playa
        public int PlaNU { get; set; }   // Playero

        // Parte de la PK (inicio del turno)
        [Required]
        public DateTime TurFyhIni { get; set; }

        public DateTime? TurFyhFin { get; set; }

        // Tiempos de apertura/cierre de caja (ajustá tipos si querés monetarios u otro sentido)
        public DateTime? TurApertCaja { get; set; }
        public DateTime? TurCierrCaja { get; set; }

        // Navegaciones
        public TrabajaEn TrabajaEn { get; set; } = default!;               // asegura la relación válida
        public PlayaEstacionamiento Playa { get; set; } = default!;        // comodidad para Include
        public Playero Playero { get; set; } = default!;                   // comodidad para Include
    }
}
