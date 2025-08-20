using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models.ViewModels
{
    public class CreateDuenioVM
    {
        [Required, StringLength(120)]
        public string UsuNyA { get; set; } = string.Empty;

        [Required, StringLength(254), EmailAddress]
        public string UsuEmail { get; set; } = string.Empty;

        [Required, StringLength(200, MinimumLength = 8), DataType(DataType.Password)]
        public string UsuPswd { get; set; } = string.Empty;

        [StringLength(30), Phone]
        public string? UsuNumTel { get; set; }

        // Específico de Dueño
        [Required]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "El CUIT debe tener 11 dígitos.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "El CUIT debe contener solo dígitos (11).")]
        public string DueCuit { get; set; } = string.Empty;
    }
}