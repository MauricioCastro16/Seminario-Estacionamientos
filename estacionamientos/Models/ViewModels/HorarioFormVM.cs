using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace estacionamientos.Models.ViewModels
{
    public class HorarioFormVM
    {
        [Required]
        public int PlyID { get; set; }

        public string PlayaNombre { get; set; } = string.Empty;
        public string? PlayaResumen { get; set; }

        [Display(Name = "Clasificacion de dias")]
        [Required(ErrorMessage = "* Debes elegir una clasificacion de dias.")]
        public int ClaDiasID { get; set; }

        [Display(Name = "Hora de apertura")]
        [DataType(DataType.Time)]
        [Required(ErrorMessage = "* Debes indicar la hora de apertura.")]
        public TimeSpan HoraApertura { get; set; }

        [Display(Name = "Hora de cierre")]
        [DataType(DataType.Time)]
        [Required(ErrorMessage = "* Debes indicar la hora de cierre.")]
        public TimeSpan HoraCierre { get; set; }

        public IEnumerable<SelectListItem> Clasificaciones { get; set; } = new List<SelectListItem>();

        public DateTime? HorFyhIniOriginal { get; set; }
        public int? ClaDiasIDOriginal { get; set; }
        public bool EsEdicion { get; set; }
    }
}
