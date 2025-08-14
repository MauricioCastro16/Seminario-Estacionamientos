namespace estacionamientos.Models
{
    // Relación N..M entre Playero y Playa via entidad propia
    public class TrabajaEn
    {
        public int PlyID { get; set; }   // FK -> PlayaEstacionamiento
        public int PlaNU { get; set; }   // FK -> Playero (UsuNU)

        // Navegaciones (no agrego colecciones en Playero/Playa para evitarte cambios)
        public PlayaEstacionamiento Playa { get; set; } = default!;
        public Playero Playero { get; set; } = default!;
    }
}
