using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models.ViewModels
{
    public class CreateConductorVM
    {
        [Required, StringLength(120)]
        public string UsuNyA { get; set; } = string.Empty;

        [Required(ErrorMessage = "* El nombre de usuario es obligatorio.")]
        [StringLength(50, ErrorMessage = "* El nombre de usuario no debe exceder los 50 caracteres.")]
        [Display(Name = "Nombre de Usuario")]
        public string UsuNomUsu { get; set; } = string.Empty;

        [Required, StringLength(254), EmailAddress]
        public string UsuEmail { get; set; } = string.Empty;

        [Required, StringLength(200, MinimumLength = 8), DataType(DataType.Password)]
        public string UsuPswd { get; set; } = string.Empty;

        [StringLength(30), Phone]
        public string? UsuNumTel { get; set; }
    }
}