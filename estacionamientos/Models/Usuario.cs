using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class Usuario
    {
        // PK
        [Key]
        public int UsuNU { get; set; }

        // Nombre y Apellido
        [Required, StringLength(120)]
        public string UsuNyA { get; set; } = string.Empty;

        [Required, StringLength(254)]
        [EmailAddress]
        public string UsuEmail { get; set; } = string.Empty;

        // Nota: en producción conviene guardar HASH, no texto plano.
        [Required, StringLength(200, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string UsuPswd { get; set; } = string.Empty;

        [StringLength(30)]
        [Phone]
        public string? UsuNumTel { get; set; }
    }
}
