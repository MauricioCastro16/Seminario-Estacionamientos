using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models;

public class PlayaEstacionamiento
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Provincia { get; set; } = default!;

    [Required, StringLength(100)]
    public string Ciudad { get; set; } = default!;

    [Required, StringLength(200)]
    public string Direccion { get; set; } = default!;

    [Required, StringLength(50)]
    public string TipoPiso { get; set; } = default!; // hormigón, asfalto, etc.

    [Range(0, double.MaxValue)]
    public decimal ValoracionPromedio { get; private set; }

    public bool LlaveRequerida { get; set; }

    public ICollection<PlazaEstacionamiento>? Plazas { get; set; }
    public ICollection<Tarifario>? Tarifas { get; set; }
    public ICollection<Servicio>? Servicios { get; set; }
}
