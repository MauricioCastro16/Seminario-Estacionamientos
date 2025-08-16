using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class Abonado
    {
        [Key]
        [StringLength(15)]
        public string AboDNI { get; set; } = "";     // PK

        [Required, StringLength(120)]
        public string AboNom { get; set; } = "";

        // 0..1 → si también es Conductor del sistema
        public int? ConNU { get; set; }              // FK opcional a Conductor (Usuario)

        // Navs
        public Conductor? Conductor { get; set; }
        public ICollection<Abono> Abonos { get; set; } = new List<Abono>();
    }
}
