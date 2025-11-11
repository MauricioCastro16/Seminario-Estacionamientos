using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models.ViewModels.Auth
{
    public class CambioContrasena
    {
        [Required(ErrorMessage = "* Este campo es obligatorio")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string ContrasenaActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "* Este campo es obligatorio")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Debe tener al menos 8 caracteres")]
        [Display(Name = "Nueva contraseña")]
        public string NuevaContrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "* Este campo es obligatorio")]
        [DataType(DataType.Password)]
        [Compare(nameof(NuevaContrasena), ErrorMessage = "Las contraseñas no coinciden")]
        [Display(Name = "Confirmar nueva contraseña")]
        public string ConfirmarNuevaContrasena { get; set; } = string.Empty;
    }
}
