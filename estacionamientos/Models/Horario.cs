using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    // Horario de atención de una Playa en una Clasificación de días
    public class Horario
    {
        // PK compuesta: PlyID + ClaDiasID + HorFyhIni
        public int PlyID { get; set; }
        public int ClaDiasID { get; set; }

        [Required]
        public DateTime HorFyhIni { get; set; }   // inicio de franja horaria (fecha+hora)

        public DateTime? HorFyhFin { get; set; } // fin de franja (opcional, p.ej. 22:00)

        // Navegaciones
        public PlayaEstacionamiento Playa { get; set; } = default!;
        public ClasificacionDias ClasificacionDias { get; set; } = default!;
    }
}
