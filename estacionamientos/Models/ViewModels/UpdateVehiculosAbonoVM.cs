namespace estacionamientos.ViewModels
{
    public class UpdateVehiculosAbonoVM
    {
        public int PlyID { get; set; }
        public int PlzNum { get; set; }
        public DateTime AboFyhIni { get; set; }

        // 🔹 Nueva propiedad para la clasificación del vehículo
        public int ClasVehID { get; set; }

        public List<VehiculoVM> Vehiculos { get; set; } = new();
    }
}
