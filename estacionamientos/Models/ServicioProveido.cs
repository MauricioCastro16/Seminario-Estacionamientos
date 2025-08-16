namespace estacionamientos.Models
{
    public class ServicioProveido
    {
        public int PlyID { get; set; }  // FK -> PlayaEstacionamiento
        public int SerID { get; set; }  // FK -> Servicio

        public bool SerProvHab { get; set; } = true;

        public PlayaEstacionamiento Playa { get; set; } = default!;
        public Servicio Servicio { get; set; } = default!;

        public ICollection<TarifaServicio> Tarifas { get; set; } = new List<TarifaServicio>();
        public ICollection<ServicioExtraRealizado> ServiciosExtras { get; set; } = new List<ServicioExtraRealizado>();
    }
}
