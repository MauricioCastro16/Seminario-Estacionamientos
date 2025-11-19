namespace estacionamientos.Models
{
    public class PlazaViewModel
    {
        public int PlyID { get; set; }
        public int PlzNum { get; set; }
        public string? PlzNombre { get; set; }
        public bool PlzHab { get; set; }
        public bool PlzOcupada { get; set; }
        public bool Techada { get; set; }
        public bool TieneAbonoActivo { get; set; }
        public string? VehPtnt { get; set; }
        public List<PlazaClasificacion> Clasificaciones { get; set; } = new List<PlazaClasificacion>();
        public int? Piso { get; set; }
    }
}