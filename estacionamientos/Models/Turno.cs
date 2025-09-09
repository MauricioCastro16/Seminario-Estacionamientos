using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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

        // --- FK al PERÍODO de TrabajaEn
        public DateTime TrabFyhIni { get; set; }   // = TrabajaEn.FechaInicio (UTC)

        //  Apertura/cierre de caja 
        [Column(TypeName = "numeric(12,2)")]
        public decimal? TurApertCaja { get; set; }    // Importe de apertura de caja

        [Column(TypeName = "numeric(12,2)")]
        public decimal? TurCierrCaja { get; set; }    // Importe real al cierre de caja

        // Navegaciones (no se validan en POST)
        [ValidateNever] public TrabajaEn TrabajaEn { get; set; } = default!;
        [ValidateNever] public PlayaEstacionamiento Playa { get; set; } = default!;
        [ValidateNever] public Playero Playero { get; set; } = default!;
    }
}
