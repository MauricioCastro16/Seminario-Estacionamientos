using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class Usuario
    {
        [Key]
        public int UsuNU { get; set; }

        [Required(ErrorMessage = "* Este campo es obligatorio")]
        [Display(Name = "Nombre y Apellido")]
        public string UsuNyA { get; set; } = string.Empty;

        [Required(ErrorMessage = "* Este campo es obligatorio")]
        [Display(Name = "Correo electrónico")]
        public string UsuEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "* Este campo es obligatorio")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string UsuPswd { get; set; } = string.Empty;

        [Display(Name = "Número de teléfono")]
        public string? UsuNumTel { get; set; }
    }
}

