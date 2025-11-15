namespace estacionamientos.Models{
    public class BreadcrumbItem
    {
        public required string Title { get; set; } //Texto que se ve en la navegación
        public string Url { get; set; } //Link al que apunta (puede ser null si es el último)
        public bool Active { get; set; } //Si es la página actual
    }
}