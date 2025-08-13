using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models;

public class Servicio
{
    public int Id { get; set; }

    [Required] public int PlayaEstacionamientoId { get; set; }
    public PlayaEstacionamiento? Playa { get; set; }

    [Required, StringLength(60)]
    public string Nombre { get; set; } = default!; // p.ej. "Lavandería", "Estacionamiento 1 hora"

    [StringLength(200)]
    public string? Descripcion { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Precio { get; set; }

    public bool Activo { get; set; } = true;
}
