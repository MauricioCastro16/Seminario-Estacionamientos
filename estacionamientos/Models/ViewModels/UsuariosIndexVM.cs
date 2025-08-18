using System.Collections.Generic;

namespace estacionamientos.Models.ViewModels
{
    public class UsuariosIndexVM
    {
        public List<Duenio> Duenios { get; set; } = new();
        public List<Conductor> Conductores { get; set; } = new();
        public List<Administrador> Administradores { get; set; } = new();
        public List<Playero> Playeros { get; set; } = new();
    }
}
