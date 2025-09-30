namespace estacionamientos.ViewModels
{
    public class AbonoCreateVM
    {
        // Datos de Abono
        public int PlyID { get; set; }
        public int PlzNum { get; set; }
        public DateTime AboFyhIni { get; set; } = DateTime.UtcNow;
        public DateTime? AboFyhFin { get; set; }

        // Datos de abonado
        public string AboDNI { get; set; } = "";
        public string AboNom { get; set; } = "";

        // Pago
        public int PagNum { get; set; }

        // Vehículos (pueden ser varios)
        public List<VehiculoVM> Vehiculos { get; set; } = new();
    }

    public class VehiculoVM
    {
        public string VehPtnt { get; set; } = "";
        public int ClasVehID { get; set; }
    }
}
