namespace estacionamientos.Models
{
    // Join Abono <-> Vehiculo
    public class VehiculoAbonado
    {
        // PK & FK al Abono
        public int PlyID { get; set; }
        public int PlzNum { get; set; }
        public DateTime AboFyhIni { get; set; }

        // PK & FK al Vehiculo
        public string VehPtnt { get; set; } = "";

        // Navs
        public Abono Abono { get; set; } = default!;
        public Vehiculo Vehiculo { get; set; } = default!;
    }
}
