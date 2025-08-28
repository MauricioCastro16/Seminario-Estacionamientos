// TrabajaEn.cs
namespace estacionamientos.Models
{
    public class TrabajaEn
    {
        public int PlyID { get; set; }
        public int PlaNU { get; set; }
        public bool TrabEnActual { get; set; } = true;

        public PlayaEstacionamiento Playa { get; set; } = default!;
        public Playero Playero { get; set; } = default!;
    }
}
