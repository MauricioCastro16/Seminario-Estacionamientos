using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models;

// Entre semana / Fin de semana (del cuadro de clasificación simple)
public enum ClasificacionDia { EntreSemana = 1, FinDeSemana = 2 }

public class Tarifario
{
    public int Id { get; set; }

    [Required] public int PlayaEstacionamientoId { get; set; }
    public PlayaEstacionamiento? Playa { get; set; }

    public ClasificacionDia Clasificacion { get; set; } = ClasificacionDia.EntreSemana;

    // Regla simple: monto por hora y fracción
    [Range(0, double.MaxValue)]
    public decimal MontoHora { get; set; }

    // fracción en minutos (ej. 15) y su precio
    [Range(1, 60)]
    public int FraccionMin { get; set; } = 15;

    [Range(0, double.MaxValue)]
    public decimal MontoFraccion { get; set; }
}
