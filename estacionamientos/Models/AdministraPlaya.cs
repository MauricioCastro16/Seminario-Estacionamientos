namespace estacionamientos.Models
{
    // Relación Dueño <-> Playa (muchos-a-muchos vía entidad propia)
    public class AdministraPlaya
    {
        public int DueNU { get; set; }   // FK a Dueno (UsuNU)
        public int PlyID { get; set; }   // FK a PlayaEstacionamiento

        public Duenio Duenio { get; set; } = default!;
        public PlayaEstacionamiento Playa { get; set; } = default!;
    }
}
