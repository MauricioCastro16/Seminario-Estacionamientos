using System;
using System.Collections.Generic;

namespace estacionamientos.Models.ViewModels
{
    public class HorariosIndexVM
    {
        public int PlyID { get; set; }
        public string PlayaNombre { get; set; } = string.Empty;
        public string? PlayaDireccion { get; set; }
        public string? PlayaCiudad { get; set; }
        public List<HorarioGroupVM> Clasificaciones { get; set; } = new();
    }

    public class HorarioGroupVM
    {
        public int ClasificacionId { get; set; }
        public string ClasificacionNombre { get; set; } = string.Empty;
        public string? ClasificacionDescripcion { get; set; }
        public List<HorarioSlotVM> Franjas { get; set; } = new();
    }

    public class HorarioSlotVM
    {
        public DateTime Inicio { get; set; }
        public DateTime? Fin { get; set; }
    }
}
