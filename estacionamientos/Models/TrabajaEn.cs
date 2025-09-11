using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class TrabajaEn
    {
        public int PlyID { get; set; }
        public int PlaNU { get; set; }
        public bool TrabEnActual { get; set; } = true;
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        // Navegaciones
        [ValidateNever] public PlayaEstacionamiento Playa { get; set; } = default!;
        [ValidateNever] public Playero Playero { get; set; } = default!;
    }
}
