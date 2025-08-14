using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace estacionamientos.Migrations
{
    /// <inheritdoc />
    public partial class prueba2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayaEstacionamiento",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlyProv = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PlyCiu = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PlyDir = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PlyTipoPiso = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PlyValProm = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false, defaultValue: 0m),
                    PlyLlavReq = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayaEstacionamiento", x => x.PlyID);
                });

            migrationBuilder.CreateTable(
                name: "UbicacionFavorita",
                columns: table => new
                {
                    ConNU = table.Column<int>(type: "integer", nullable: false),
                    UbfApodo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UbfProv = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UbfCiu = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    UbfDir = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UbfTipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UbicacionFavorita", x => new { x.ConNU, x.UbfApodo });
                    table.ForeignKey(
                        name: "FK_UbicacionFavorita_Conductor_ConNU",
                        column: x => x.ConNU,
                        principalTable: "Conductor",
                        principalColumn: "UsuNU",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdministraPlaya",
                columns: table => new
                {
                    DueNU = table.Column<int>(type: "integer", nullable: false),
                    PlyID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdministraPlaya", x => new { x.DueNU, x.PlyID });
                    table.ForeignKey(
                        name: "FK_AdministraPlaya_Duenio_DueNU",
                        column: x => x.DueNU,
                        principalTable: "Duenio",
                        principalColumn: "UsuNU",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdministraPlaya_PlayaEstacionamiento_PlyID",
                        column: x => x.PlyID,
                        principalTable: "PlayaEstacionamiento",
                        principalColumn: "PlyID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Valoracion",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    ConNU = table.Column<int>(type: "integer", nullable: false),
                    ValNumEst = table.Column<int>(type: "integer", nullable: false),
                    ValFav = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Valoracion", x => new { x.PlyID, x.ConNU });
                    table.ForeignKey(
                        name: "FK_Valoracion_Conductor_ConNU",
                        column: x => x.ConNU,
                        principalTable: "Conductor",
                        principalColumn: "UsuNU",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Valoracion_PlayaEstacionamiento_PlyID",
                        column: x => x.PlyID,
                        principalTable: "PlayaEstacionamiento",
                        principalColumn: "PlyID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdministraPlaya_PlyID",
                table: "AdministraPlaya",
                column: "PlyID");

            migrationBuilder.CreateIndex(
                name: "IX_Valoracion_ConNU",
                table: "Valoracion",
                column: "ConNU");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdministraPlaya");

            migrationBuilder.DropTable(
                name: "UbicacionFavorita");

            migrationBuilder.DropTable(
                name: "Valoracion");

            migrationBuilder.DropTable(
                name: "PlayaEstacionamiento");
        }
    }
}
