using Microsoft.AspNetCore.Mvc;
using estacionamientos.Models;

namespace estacionamientos.Controllers{
    public abstract class BaseController : Controller
    {
        protected void SetBreadcrumb(params BreadcrumbItem[] items)
        {
            var breadcrumb = new List<BreadcrumbItem>
            {
                new BreadcrumbItem 
                { 
                    Title = "Inicio", 
                    Url = "/", 
                    Active = false 
                }
            };

            if (items != null && items.Length > 0)
            {
                // Agregamos todos los items pasados
                breadcrumb.AddRange(items);

                // Marcamos el último como activo
                breadcrumb[breadcrumb.Count - 1].Active = true;
            }

            ViewBag.Breadcrumb = breadcrumb;
        }
    }
}