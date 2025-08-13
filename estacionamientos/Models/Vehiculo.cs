using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models;

public enum TipoVehiculo { Auto = 1, Camioneta = 2, Moto = 3, Otro = 99 }

public class Vehiculo
{
    public int Id { get; set; }

    [Required, StringLength(10)]
    public string Patente { get; set; } = default!;

    [StringLength(50)] public string? Marca { get; set; }
    [StringLength(50)] public string? Modelo { get; set; }
    [StringLength(30)] public string? Color { get; set; }

    public TipoVehiculo Tipo { get; set; } = TipoVehiculo.Auto;

    public ICollection<Ocupacion>? Ocupaciones { get; set; }
}
