using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    // Hereda de Usuario (ya definido)
    public class Duenio : Usuario
    {
        // CUIT argentino: 11 dígitos. Guardamos string para preservar ceros a la izquierda.
        [Required]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "* El CUIT debe tener 11 dígitos.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "* El CUIT debe contener solo dígitos (11).")]
        public string DueCuit { get; set; } = string.Empty;
        public ICollection<AdministraPlaya> Administraciones { get; set; } = new List<AdministraPlaya>();

    }
}
