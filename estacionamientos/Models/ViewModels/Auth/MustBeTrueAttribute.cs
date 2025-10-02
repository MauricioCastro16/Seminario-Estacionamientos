using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models.ViewModels.Auth
{
    public class MustBeTrueAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            return value is bool boolValue && boolValue;
        }
    }
}
