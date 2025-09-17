namespace estacionamientos.Models.ViewModels
{
    public class TarifasIndexVM
        {
            public string Q { get; set; } = "";
            public string FilterBy { get; set; } = "all";
            public List<string> Playas { get; set; } = new();
            public List<string> Servicios { get; set; } = new();
            public List<string> Clases { get; set; } = new();
            public List<string> Vigencias { get; set; } = new();   // 👈 agregado
            public List<string> Todos { get; set; } = new();

            public string? SelectedOption { get; set; }   // 👈 opción elegida en el dropdown de vigencia

            public List<TarifaServicio> Tarifas { get; set; } = new();
        }

}
