using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    // Hereda toda la info de Usuario
    public class Playero : Usuario
    {
        // relacion con movimientos
        public ICollection<MovimientoPlayero> Movimientos { get; set; } = new List<MovimientoPlayero>();
    }
}
