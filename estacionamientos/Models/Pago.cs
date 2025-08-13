using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models;

public enum MetodoPago { Debito = 1, Credito = 2, QR = 3, Efectivo = 4 }

public class Pago
{
    public int Id { get; set; }

    [Required] public int OcupacionId { get; set; }
    public Ocupacion? Ocupacion { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Monto { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public MetodoPago Metodo { get; set; } = MetodoPago.Debito;

    [StringLength(80)]
    public string? Autorizacion { get; set; } // código de aprobación, txn id, etc.
}
