using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{

     public enum EstadoPago
    {
        Activo,     // al día - tiene el periodo actual de abono pagado
        Pendiente,  // según la fecha le faltan pagos
        Finalizado, // se llegó al fin del abono con todo pagado
        Cancelado   // se canceló manualmente o por vencimiento del pago
    }

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

        [Required]
        public EstadoPago EstadoPago { get; set; } = EstadoPago.Activo;

        // Navs
        public PlazaEstacionamiento Plaza { get; set; } = default!;
        public Abonado Abonado { get; set; } = default!;
        public Pago Pago { get; set; } = default!;
        public ICollection<VehiculoAbonado> Vehiculos { get; set; } = new List<VehiculoAbonado>();
        public ICollection<PeriodoAbono> Periodos { get; set; } = new List<PeriodoAbono>();
    }
}
