using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace estacionamientos.Migrations
{
    /// <inheritdoc />
    public partial class Valores_por_defecto_en_MetodoPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MetodoPago",
                columns: new[] { "MepID", "MepDesc", "MepNom" },
                values: new object[,]
                {
                    { 1, "Pago en efectivo", "Efectivo" },
                    { 2, "Visa, Mastercard, etc.", "Tarjeta de Crédito" },
                    { 3, "Pago con tarjeta de débito", "Tarjeta de Débito" },
                    { 4, "Mediante el uso de un QR", "QR" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MetodoPago",
                keyColumn: "MepID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "MetodoPago",
                keyColumn: "MepID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "MetodoPago",
                keyColumn: "MepID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "MetodoPago",
                keyColumn: "MepID",
                keyValue: 4);
        }
    }
}
