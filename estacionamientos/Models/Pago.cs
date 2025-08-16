using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class Pago
    {
        // PK compuesta: (PlyID, PagNum)
        public int PlyID { get; set; }
        public int PagNum { get; set; }

        // FK al método usado (parte de la FK compuesta hacia AceptaMetodoPago)
        public int MepID { get; set; }

        [Required]
        public decimal PagMonto { get; set; }

        [Required]
        public DateTime PagFyh { get; set; } = DateTime.Now;

        // Navs
        public PlayaEstacionamiento Playa { get; set; } = default!;
        public MetodoPago MetodoPago { get; set; } = default!;
        public AceptaMetodoPago AceptaMetodoPago { get; set; } = default!; // (PlyID, MepID)
    }
}
