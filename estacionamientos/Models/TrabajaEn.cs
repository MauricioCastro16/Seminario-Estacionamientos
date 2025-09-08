using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace estacionamientos.Models
{
    public class TrabajaEn
    {
        public int PlyID { get; set; }
        public int PlaNU { get; set; }
        public bool TrabEnActual { get; set; } = true;

        // Navegaciones (no se validan en los formularios)
        [ValidateNever]
        public PlayaEstacionamiento Playa { get; set; } = default!;

        [ValidateNever]
        public Playero Playero { get; set; } = default!;
    }
}