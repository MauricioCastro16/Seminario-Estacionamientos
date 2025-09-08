using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models.ViewModels.Auth
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "* El Email es obligatorio.")]
        [EmailAddress(ErrorMessage = "* Ingresá un Email válido.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "* La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Recordarme")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
