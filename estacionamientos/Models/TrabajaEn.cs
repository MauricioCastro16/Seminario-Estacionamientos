using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace estacionamientos.Models
{
    // Relación N..M entre Playero y Playa via entidad propia
    public class TrabajaEn
    {
        public int PlyID { get; set; }   // FK -> PlayaEstacionamiento
        public int PlaNU { get; set; }   // FK -> Playero (UsuNU)

        // Navegaciones (no se validan en los formularios)
        [ValidateNever]
        public PlayaEstacionamiento Playa { get; set; } = default!;

        [ValidateNever]
        public Playero Playero { get; set; } = default!;
    }
}