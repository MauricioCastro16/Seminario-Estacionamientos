namespace estacionamientos.Models
{
    // Qué métodos acepta cada playa (PK compuesta)
    public class AceptaMetodoPago
    {
        public int PlyID { get; set; }
        public int MepID { get; set; }

        public bool AmpHab { get; set; } = true; // habilitado/deshabilitado

        public PlayaEstacionamiento? Playa { get; set; } = default!;
        public MetodoPago? MetodoPago { get; set; } = default!;

        public ICollection<Pago>? Pagos { get; set; } = new List<Pago>(); // comodidad
    }
}
