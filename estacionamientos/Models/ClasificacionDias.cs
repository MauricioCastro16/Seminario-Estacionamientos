using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class ClasificacionDias
    {
        [Key]
        public int ClaDiasID { get; set; }          // PK

        [Required, StringLength(40)]
        public string ClaDiasTipo { get; set; } = ""; // p.ej. "Entre semana", "Fin de semana"

        [StringLength(200)]
        public string? ClaDiasDesc { get; set; }

        public ICollection<Horario> Horarios { get; set; } = new List<Horario>();
    }
}
