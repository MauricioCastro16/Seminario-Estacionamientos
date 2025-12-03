using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models.ViewModels.Auth
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "* El nombre y apellido es obligatorio.")]
        [StringLength(120, ErrorMessage = "* El nombre no debe exceder los 120 caracteres.")]
        [Display(Name = "Nombre y Apellido")]
        public string UsuNyA { get; set; } = string.Empty;

        [Required(ErrorMessage = "* El nombre de usuario es obligatorio.")]
        [StringLength(50, ErrorMessage = "* El nombre de usuario no debe exceder los 50 caracteres.")]
        [Display(Name = "Nombre de Usuario")]
        public string UsuNomUsu { get; set; } = string.Empty;

        [Required(ErrorMessage = "* El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "* Ingrese un correo electrónico válido.")]
        [StringLength(254, ErrorMessage = "* El correo no debe exceder los 254 caracteres.")]
        [Display(Name = "Correo Electrónico")]
        public string UsuEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "* La contraseña es obligatoria.")]
        [StringLength(200, MinimumLength = 8, ErrorMessage = "* La contraseña debe tener entre 8 y 200 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string UsuPswd { get; set; } = string.Empty;

        [Required(ErrorMessage = "* La confirmación de contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Contraseña")]
        [Compare("UsuPswd", ErrorMessage = "* Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone(ErrorMessage = "* Ingrese un número de teléfono válido.")]
        [StringLength(30, ErrorMessage = "* El número de teléfono no debe exceder los 30 caracteres.")]
        [Display(Name = "Número de Teléfono (Opcional)")]
        public string? UsuNumTel { get; set; }

        [Required(ErrorMessage = "* El DNI es obligatorio.")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "* El DNI debe tener exactamente 8 dígitos.")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "* El DNI debe contener solo números y tener 8 dígitos.")]
        [Display(Name = "DNI")]
        public string ConDNI { get; set; } = string.Empty;

        [Display(Name = "Acepto los términos y condiciones")]
        [MustBeTrue(ErrorMessage = "* Debe aceptar los términos y condiciones.")]
        public bool AcceptTerms { get; set; }
    }
}
