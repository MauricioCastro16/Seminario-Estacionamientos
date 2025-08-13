using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models;

public class PlazaEstacionamiento
{
    public int Id { get; set; }

    [Required]
    public int PlayaEstacionamientoId { get; set; }
    public PlayaEstacionamiento? Playa { get; set; }

    [Required, StringLength(30)]
    public string Codigo { get; set; } = default!; // ej. A-12

    [StringLength(30)]
    public string? Nivel { get; set; } // sub1, PB, 1°, etc.

    [StringLength(30)]
    public string? Sector { get; set; } // zona A, zona B...

    public bool EsCubierta { get; set; }
    public bool EsReservada { get; set; }
    public bool Activa { get; set; } = true;

    public ICollection<Ocupacion>? Ocupaciones { get; set; }
}
