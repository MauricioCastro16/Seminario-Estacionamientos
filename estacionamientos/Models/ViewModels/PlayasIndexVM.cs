// Models/ViewModels/PlayasIndexVM.cs
using System.Collections.Generic;
using estacionamientos.Models;

namespace estacionamientos.Models.ViewModels
{
    public class PlayasIndexVM
    {
        public string? Q { get; set; }
        public string FilterBy { get; set; } = "all";
        public string? SelectedOption { get; set; }


        public List<string> Nombres { get; set; } = new();
        public List<string> Provincias { get; set; } = new();
        public List<string> Ciudades { get; set; } = new();
        public List<string> Direcciones { get; set; } = new();
        public List<string> Todos { get; set; } = new();

        public string? Remove { get; set; }


        public List<string> ProvinciasCombo { get; set; } = new();
        public List<PlayaEstacionamiento> Playas { get; set; } = new();

        public bool HayFiltros =>
            FilterBy != "all" ||
            !string.IsNullOrWhiteSpace(Q) ||
            !string.IsNullOrWhiteSpace(SelectedOption) ||
            Nombres.Count > 0 || Provincias.Count > 0 || Ciudades.Count > 0 || Direcciones.Count > 0 || Todos.Count > 0;
    }
}

