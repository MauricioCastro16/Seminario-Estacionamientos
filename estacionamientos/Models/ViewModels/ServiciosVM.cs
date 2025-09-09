using estacionamientos.Models;
namespace estacionamientos.ViewModels;
public class ServiciosViewModel
{
    public int PlayaID { get; set; }
    public string PlayaNom { get; set; }
    public List<Servicio> ServiciosDisponibles { get; set; } = new List<Servicio>();
    public List<int> ServiciosAsignados { get; set; } = new List<int>();  // Servicios ya asignados a la playa
}
