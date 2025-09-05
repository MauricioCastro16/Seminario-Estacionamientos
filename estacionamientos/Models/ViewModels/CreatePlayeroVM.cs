using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models.ViewModels
{
    public class CreatePlayeroVM
    {
        [Required(ErrorMessage = "* Este campo es obligatorio")]
        public string UsuNyA { get; set; } = string.Empty;

        [Required(ErrorMessage = "* Este campo es obligatorio")]
        public string UsuEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "* Este campo es obligatorio")]
        [DataType(DataType.Password)]
        public string UsuPswd { get; set; } = string.Empty;

        public string? UsuNumTel { get; set; }
    }
}
