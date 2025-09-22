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
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        [Display(Name = "Contraseña")]
        public string UsuPswd { get; set; } = string.Empty;

        [Display(Name = "Número de teléfono")]
        public string? UsuNumTel { get; set; }

        [Required(ErrorMessage = "* El nombre de usuario es obligatorio.")]
        [StringLength(50, ErrorMessage = "* El nombre de usuario no debe exceder los 50 caracteres.")]
        [Display(Name = "Nombre de Usuario")]
        public string UsuNomUsu { get; set; } = string.Empty;  // Nombre de usuario obligatorio
    }
}