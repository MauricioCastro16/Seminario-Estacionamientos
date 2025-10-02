using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class Servicio
    {
        [Key]
        public int SerID { get; set; }

        [Required, StringLength(80)]
        public string SerNom { get; set; } = "";

        [StringLength(40)]
        public string? SerTipo { get; set; }   // ej: "Estacionamiento", "Lavado"

        [StringLength(200)]
        public string? SerDesc { get; set; }

        public int? SerDuracionMinutos { get; set; }

        public ICollection<ServicioProveido> Proveidos { get; set; } = new List<ServicioProveido>();
    }
}
