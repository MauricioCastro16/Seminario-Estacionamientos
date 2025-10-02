namespace estacionamientos.Helpers
{
    public static class ErrorMessages
    {
        public const string CampoObligatorio = "* Este campo es obligatorio";
        public const string CantidadMayorCero = "* La cantidad debe ser mayor a 0";
        public const string DNIInvalido = "* El DNI debe tener entre 7 y 11 dígitos";
        public const string NombreExcedeCaracteres = "* El nombre no debe exceder los 120 caracteres";
        public const string PatenteInvalida = "* La patente debe tener entre 6 y 10 caracteres";
        public const string SeleccioneMetodoPago = "* Seleccione un método de pago";
        public const string SeleccioneTipoAbono = "* Seleccione un tipo de abono";
        public const string SeleccioneClasificacionVehiculo = "* Seleccione una clasificación de vehículo";
        public const string SeleccionePlaza = "* Debe seleccionar una plaza para el abono";
    }
}