using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models;

public class Ocupacion
{
    public int Id { get; set; }

    [Required] public int PlazaEstacionamientoId { get; set; }
    public PlazaEstacionamiento? Plaza { get; set; }

    [Required] public int VehiculoId { get; set; }
    public Vehiculo? Vehiculo { get; set; }

    public int? ConductorId { get; set; }
    public Conductor? Conductor { get; set; }

    public DateTime HoraEntrada { get; set; } = DateTime.UtcNow;
    public DateTime? HoraSalida { get; set; }

    // valores calculados/registrados
    public decimal? ImporteCalculado { get; set; }

    public Pago? Pago { get; set; } // 1-1
}
