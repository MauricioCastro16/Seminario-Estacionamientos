using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class Abono
    {
        // PK compuesta e identificador natural del abono
        public int PlyID { get; set; }
        public int PlzNum { get; set; }
        [Required]
        public DateTime AboFyhIni { get; set; }      // inicio del abono (parte de la PK)

        public DateTime? AboFyhFin { get; set; }

        public decimal AboMonto { get; set; }

        // FK requeridas
        [Required]
        [StringLength(15)]
        public string AboDNI { get; set; } = "";     // -> Abonado

        [Required]
        public int PagNum { get; set; }              // -> Pago (PlyID, PagNum) requerido

        // Navs
        public PlazaEstacionamiento Plaza { get; set; } = default!;
        public Abonado Abonado { get; set; } = default!;
        public Pago Pago { get; set; } = default!;
        public ICollection<VehiculoAbonado> Vehiculos { get; set; } = new List<VehiculoAbonado>();
    }
}
