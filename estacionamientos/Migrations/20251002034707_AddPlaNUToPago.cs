using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace estacionamientos.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaNUToPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlaNU",
                table: "Pago",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaNU",
                table: "Pago");
        }
    }
}
