using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models;

public class Conductor
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Nombre { get; set; } = default!;

    [Required, StringLength(100)]
    public string Apellido { get; set; } = default!;

    [StringLength(20)]
    public string? Documento { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? Telefono { get; set; }

    public ICollection<Ocupacion>? Ocupaciones { get; set; }
}
