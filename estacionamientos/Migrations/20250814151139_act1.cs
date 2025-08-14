using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace estacionamientos.Migrations
{
    /// <inheritdoc />
    public partial class act1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClasificacionVehiculo",
                columns: table => new
                {
                    ClasVehID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClasVehTipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ClasVehDesc = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClasificacionVehiculo", x => x.ClasVehID);
                });

            migrationBuilder.CreateTable(
                name: "Vehiculo",
                columns: table => new
                {
                    VehPtnt = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    VehMarc = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ClasVehID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehiculo", x => x.VehPtnt);
                    table.ForeignKey(
                        name: "FK_Vehiculo_ClasificacionVehiculo_ClasVehID",
                        column: x => x.ClasVehID,
                        principalTable: "ClasificacionVehiculo",
                        principalColumn: "ClasVehID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Conduce",
                columns: table => new
                {
                    ConNU = table.Column<int>(type: "integer", nullable: false),
                    VehPtnt = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conduce", x => new { x.ConNU, x.VehPtnt });
                    table.ForeignKey(
                        name: "FK_Conduce_Conductor_ConNU",
                        column: x => x.ConNU,
                        principalTable: "Conductor",
                        principalColumn: "UsuNU",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Conduce_Vehiculo_VehPtnt",
                        column: x => x.VehPtnt,
                        principalTable: "Vehiculo",
                        principalColumn: "VehPtnt",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClasificacionVehiculo_ClasVehTipo",
                table: "ClasificacionVehiculo",
                column: "ClasVehTipo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conduce_VehPtnt",
                table: "Conduce",
                column: "VehPtnt");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculo_ClasVehID",
                table: "Vehiculo",
                column: "ClasVehID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Conduce");

            migrationBuilder.DropTable(
                name: "Vehiculo");

            migrationBuilder.DropTable(
                name: "ClasificacionVehiculo");
        }
    }
}
