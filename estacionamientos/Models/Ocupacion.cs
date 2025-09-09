using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using estacionamientos.Helpers;

namespace estacionamientos.Models
{
    public class Ocupacion
    {
        // PK compuesta
        [Display(Name = "Playa")]
        public int PlyID { get; set; }

        [Required(ErrorMessage = ErrorMessages.CampoObligatorio)]
        [Display(Name = "Plaza")]
        public int? PlzNum { get; set; }   // 🔹 ahora nullable

        [Required(ErrorMessage = ErrorMessages.CampoObligatorio)]
        [StringLength(10, ErrorMessage = "La patente no puede superar los 10 caracteres")]
        [Display(Name = "Patente")]
        public string VehPtnt { get; set; } = string.Empty;

        [Display(Name = "Hora de ingreso")]
        public DateTime OcufFyhIni { get; set; }

        [Display(Name = "Hora de egreso")]
        public DateTime? OcufFyhFin { get; set; }

        [Display(Name = "Dejó llaves")]
        public bool OcuLlavDej { get; set; }

        public int? PagNum { get; set; }

        // 🔹 Navegación → que NO se valide en el form
        [BindNever]
        public PlazaEstacionamiento? Plaza { get; set; }

        [BindNever]
        public Vehiculo? Vehiculo { get; set; }

        [BindNever]
        public Pago? Pago { get; set; }
    }
}
