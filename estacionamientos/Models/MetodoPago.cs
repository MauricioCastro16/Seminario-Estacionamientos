using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class MetodoPago
    {
        [Key]
        public int MepID { get; set; }

        [Required, StringLength(40)]
        public string MepNom { get; set; } = "";

        [StringLength(200)]
        public string? MepDesc { get; set; }

        public ICollection<AceptaMetodoPago> Aceptaciones { get; set; } = new List<AceptaMetodoPago>();
    }
}
