using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // <- para Column()
using estacionamientos.Helpers;

namespace estacionamientos.Models
{
    public class PlayaEstacionamiento
    {
        [Key]
        public int PlyID { get; set; } // Identity

        [Required(ErrorMessage = ErrorMessages.CampoObligatorio), StringLength(50)]
        public string PlyNom { get; set; } = string.Empty;

        // Nombre de provincia (texto mostrado al usuario)
        [Required(ErrorMessage = ErrorMessages.CampoObligatorio), StringLength(50)]
        public string PlyProv { get; set; } = string.Empty;

        // (Opcional) ID oficial de provincia según la API georef (p. ej. "22" para Chaco)
        [StringLength(10)]
        public string? PlyProvId { get; set; }

        // Nombre de ciudad / localidad (texto mostrado al usuario)
        [Required(ErrorMessage = ErrorMessages.CampoObligatorio), StringLength(80)]
        public string PlyCiu { get; set; } = string.Empty;

        // (Opcional) ID oficial de localidad según la API georef (p. ej. "220161" para Makallé)
        [StringLength(12)]
        public string? PlyCiuId { get; set; }

        [Required(ErrorMessage = ErrorMessages.CampoObligatorio), StringLength(120)]
        public string PlyDir { get; set; } = string.Empty;

        [Required(ErrorMessage = ErrorMessages.CampoObligatorio), StringLength(30)]
        public string PlyTipoPiso { get; set; } = string.Empty; // hormigón, ripio, tierra…

        // Promedio de Valoracion.ValNumEst (persistido)
        public decimal PlyValProm { get; set; } // 0..5 por ejemplo

        public bool PlyLlavReq { get; set; } // ¿requiere dejar llaves?

        // ===== NUEVO: coordenadas seleccionadas en el mapa =====
        public decimal? PlyLat { get; set; }

        public decimal? PlyLon { get; set; }

        // Navegaciones
        public ICollection<Valoracion> Valoraciones { get; set; } = new List<Valoracion>();
        public ICollection<AdministraPlaya> Administradores { get; set; } = new List<AdministraPlaya>();
        public ICollection<Horario> Horarios { get; set; } = new List<Horario>();
        public ICollection<AceptaMetodoPago> Aceptaciones { get; set; } = new List<AceptaMetodoPago>();
        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
        public ICollection<PlazaEstacionamiento> Plazas { get; set; } = new List<PlazaEstacionamiento>();
        public ICollection<ServicioProveido> ServiciosProveidos { get; set; } = new List<ServicioProveido>();

        public ICollection<MovimientoPlayero> Movimientos { get; set; } = new List<MovimientoPlayero>();
    }
}
