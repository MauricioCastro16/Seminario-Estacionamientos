using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models;

public class PlayaEstacionamiento
{
    [Key]
    public int PlyID { get; set; }

    [Required, StringLength(100)]
    public string PlyProv { get; set; } = default!; // Provincia

    [Required, StringLength(100)]
    public string PlyCiu { get; set; } = default!; // Ciudad

    [Required, StringLength(200)]
    public string PlyDir { get; set; } = default!; // Dirección

    [Required, StringLength(50)]
    public string PlyTipoPiso { get; set; } = default!; // Tipo de piso

    [Range(0, double.MaxValue)]
    public decimal PlyValProm { get; set; } // Valor promedio

    public bool PlyLlavReq { get; set; } // ¿Requiere dejar llave?
}
