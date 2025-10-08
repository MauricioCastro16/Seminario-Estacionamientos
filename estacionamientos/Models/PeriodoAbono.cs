using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace estacionamientos.Models
{
    public class PeriodoAbono
    {
        // PK compuesta
        [Key, Column(Order = 0)]
        public int PlyID { get; set; }

        [Key, Column(Order = 1)]
        public int PlzNum { get; set; }

        [Key, Column(Order = 2)]
        public DateTime AboFyhIni { get; set; }

        [Key, Column(Order = 3)]
        public int PeriodoNumero { get; set; }

        // Datos del período
        public DateTime PeriodoFechaInicio { get; set; }
        public DateTime PeriodoFechaFin { get; set; }
        public decimal PeriodoMonto { get; set; }
        public bool PeriodoPagado { get; set; } = false;
        public DateTime? PeriodoFechaPago { get; set; }

        // 🔹 Nueva FK opcional al pago
        public int? PagNum { get; set; }

        // Navigation properties
        public Abono Abono { get; set; } = default!;
        public Pago? Pago { get; set; }   // 🔹 un pago opcional por período

        // 🔹 Estado calculado del período (no se guarda en la BD)
        [NotMapped]
        public string EstadoPeriodo
        {
            get
            {
                var hoy = DateTime.UtcNow.Date;

                if (PeriodoPagado)
                    return "Pagado";

                // Si el período aún no venció, está al día
                if (PeriodoFechaFin.Date >= hoy)
                    return "Al día";

                // Si ya venció y no está pagado
                return "Pendiente";
            }
        }

    }
}
