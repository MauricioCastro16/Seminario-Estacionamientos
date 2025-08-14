using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class PlayaEstacionamiento
    {
        [Key]
        public int PlyID { get; set; } // Identity

        [Required, StringLength(50)]
        public string PlyProv { get; set; } = string.Empty;

        [Required, StringLength(80)]
        public string PlyCiu { get; set; } = string.Empty;

        [Required, StringLength(120)]
        public string PlyDir { get; set; } = string.Empty;

        [StringLength(30)]
        public string? PlyTipoPiso { get; set; } // hormigón, ripio, tierra…

        // Promedio de Valoracion.ValNumEst (persistido)
        public decimal PlyValProm { get; set; } // 0..5 por ejemplo

        public bool PlyLlavReq { get; set; } // ¿requiere dejar llaves?

        // Navegaciones
        public ICollection<Valoracion> Valoraciones { get; set; } = new List<Valoracion>();
        public ICollection<AdministraPlaya> Administradores { get; set; } = new List<AdministraPlaya>();
    }
}
