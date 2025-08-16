using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace estacionamientos.Migrations
{
    /// <inheritdoc />
    public partial class prueba3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClasificacionDias",
                columns: table => new
                {
                    ClaDiasID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClaDiasTipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ClaDiasDesc = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClasificacionDias", x => x.ClaDiasID);
                });

            migrationBuilder.CreateTable(
                name: "MetodoPago",
                columns: table => new
                {
                    MepID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MepNom = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MepDesc = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetodoPago", x => x.MepID);
                });

            migrationBuilder.CreateTable(
                name: "PlazaEstacionamiento",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    PlzNum = table.Column<int>(type: "integer", nullable: false),
                    PlzOcupada = table.Column<bool>(type: "boolean", nullable: false),
                    PlzTecho = table.Column<bool>(type: "boolean", nullable: false),
                    PlzAlt = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    PlzHab = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlazaEstacionamiento", x => new { x.PlyID, x.PlzNum });
                    table.ForeignKey(
                        name: "FK_PlazaEstacionamiento_PlayaEstacionamiento_PlyID",
                        column: x => x.PlyID,
                        principalTable: "PlayaEstacionamiento",
                        principalColumn: "PlyID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrabajaEn",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    PlaNU = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrabajaEn", x => new { x.PlyID, x.PlaNU });
                    table.ForeignKey(
                        name: "FK_TrabajaEn_PlayaEstacionamiento_PlyID",
                        column: x => x.PlyID,
                        principalTable: "PlayaEstacionamiento",
                        principalColumn: "PlyID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrabajaEn_Playero_PlaNU",
                        column: x => x.PlaNU,
                        principalTable: "Playero",
                        principalColumn: "UsuNU",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Horario",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    ClaDiasID = table.Column<int>(type: "integer", nullable: false),
                    HorFyhIni = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HorFyhFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Horario", x => new { x.PlyID, x.ClaDiasID, x.HorFyhIni });
                    table.ForeignKey(
                        name: "FK_Horario_ClasificacionDias_ClaDiasID",
                        column: x => x.ClaDiasID,
                        principalTable: "ClasificacionDias",
                        principalColumn: "ClaDiasID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Horario_PlayaEstacionamiento_PlyID",
                        column: x => x.PlyID,
                        principalTable: "PlayaEstacionamiento",
                        principalColumn: "PlyID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AceptaMetodoPago",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    MepID = table.Column<int>(type: "integer", nullable: false),
                    AmpHab = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AceptaMetodoPago", x => new { x.PlyID, x.MepID });
                    table.ForeignKey(
                        name: "FK_AceptaMetodoPago_MetodoPago_MepID",
                        column: x => x.MepID,
                        principalTable: "MetodoPago",
                        principalColumn: "MepID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AceptaMetodoPago_PlayaEstacionamiento_PlyID",
                        column: x => x.PlyID,
                        principalTable: "PlayaEstacionamiento",
                        principalColumn: "PlyID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Turno",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    PlaNU = table.Column<int>(type: "integer", nullable: false),
                    TurFyhIni = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TurFyhFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TurApertCaja = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TurCierrCaja = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turno", x => new { x.PlyID, x.PlaNU, x.TurFyhIni });
                    table.ForeignKey(
                        name: "FK_Turno_PlayaEstacionamiento_PlyID",
                        column: x => x.PlyID,
                        principalTable: "PlayaEstacionamiento",
                        principalColumn: "PlyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Turno_Playero_PlaNU",
                        column: x => x.PlaNU,
                        principalTable: "Playero",
                        principalColumn: "UsuNU",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Turno_TrabajaEn_PlyID_PlaNU",
                        columns: x => new { x.PlyID, x.PlaNU },
                        principalTable: "TrabajaEn",
                        principalColumns: new[] { "PlyID", "PlaNU" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pago",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    PagNum = table.Column<int>(type: "integer", nullable: false),
                    MepID = table.Column<int>(type: "integer", nullable: false),
                    PagMonto = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    PagFyh = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlayaEstacionamientoPlyID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pago", x => new { x.PlyID, x.PagNum });
                    table.ForeignKey(
                        name: "FK_Pago_AceptaMetodoPago_PlyID_MepID",
                        columns: x => new { x.PlyID, x.MepID },
                        principalTable: "AceptaMetodoPago",
                        principalColumns: new[] { "PlyID", "MepID" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pago_MetodoPago_MepID",
                        column: x => x.MepID,
                        principalTable: "MetodoPago",
                        principalColumn: "MepID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pago_PlayaEstacionamiento_PlayaEstacionamientoPlyID",
                        column: x => x.PlayaEstacionamientoPlyID,
                        principalTable: "PlayaEstacionamiento",
                        principalColumn: "PlyID");
                    table.ForeignKey(
                        name: "FK_Pago_PlayaEstacionamiento_PlyID",
                        column: x => x.PlyID,
                        principalTable: "PlayaEstacionamiento",
                        principalColumn: "PlyID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ocupacion",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    PlzNum = table.Column<int>(type: "integer", nullable: false),
                    VehPtnt = table.Column<string>(type: "character varying(10)", nullable: false),
                    OcufFyhIni = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OcufFyhFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OcuLlavDej = table.Column<bool>(type: "boolean", nullable: false),
                    PagNum = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ocupacion", x => new { x.PlyID, x.PlzNum, x.VehPtnt, x.OcufFyhIni });
                    table.ForeignKey(
                        name: "FK_Ocupacion_Pago_PlyID_PagNum",
                        columns: x => new { x.PlyID, x.PagNum },
                        principalTable: "Pago",
                        principalColumns: new[] { "PlyID", "PagNum" },
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Ocupacion_PlazaEstacionamiento_PlyID_PlzNum",
                        columns: x => new { x.PlyID, x.PlzNum },
                        principalTable: "PlazaEstacionamiento",
                        principalColumns: new[] { "PlyID", "PlzNum" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ocupacion_Vehiculo_VehPtnt",
                        column: x => x.VehPtnt,
                        principalTable: "Vehiculo",
                        principalColumn: "VehPtnt",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AceptaMetodoPago_MepID",
                table: "AceptaMetodoPago",
                column: "MepID");

            migrationBuilder.CreateIndex(
                name: "IX_ClasificacionDias_ClaDiasTipo",
                table: "ClasificacionDias",
                column: "ClaDiasTipo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Horario_ClaDiasID",
                table: "Horario",
                column: "ClaDiasID");

            migrationBuilder.CreateIndex(
                name: "IX_Horario_PlyID_ClaDiasID_HorFyhIni",
                table: "Horario",
                columns: new[] { "PlyID", "ClaDiasID", "HorFyhIni" });

            migrationBuilder.CreateIndex(
                name: "IX_MetodoPago_MepNom",
                table: "MetodoPago",
                column: "MepNom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ocupacion_PlyID_PagNum",
                table: "Ocupacion",
                columns: new[] { "PlyID", "PagNum" });

            migrationBuilder.CreateIndex(
                name: "IX_Ocupacion_PlyID_PlzNum_OcufFyhIni",
                table: "Ocupacion",
                columns: new[] { "PlyID", "PlzNum", "OcufFyhIni" });

            migrationBuilder.CreateIndex(
                name: "IX_Ocupacion_VehPtnt_OcufFyhIni",
                table: "Ocupacion",
                columns: new[] { "VehPtnt", "OcufFyhIni" });

            migrationBuilder.CreateIndex(
                name: "IX_Pago_MepID",
                table: "Pago",
                column: "MepID");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_PlayaEstacionamientoPlyID",
                table: "Pago",
                column: "PlayaEstacionamientoPlyID");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_PlyID_MepID",
                table: "Pago",
                columns: new[] { "PlyID", "MepID" });

            migrationBuilder.CreateIndex(
                name: "IX_Pago_PlyID_PagFyh",
                table: "Pago",
                columns: new[] { "PlyID", "PagFyh" });

            migrationBuilder.CreateIndex(
                name: "IX_TrabajaEn_PlaNU",
                table: "TrabajaEn",
                column: "PlaNU");

            migrationBuilder.CreateIndex(
                name: "IX_Turno_PlaNU",
                table: "Turno",
                column: "PlaNU");

            migrationBuilder.CreateIndex(
                name: "IX_Turno_PlyID_TurFyhIni",
                table: "Turno",
                columns: new[] { "PlyID", "TurFyhIni" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Horario");

            migrationBuilder.DropTable(
                name: "Ocupacion");

            migrationBuilder.DropTable(
                name: "Turno");

            migrationBuilder.DropTable(
                name: "ClasificacionDias");

            migrationBuilder.DropTable(
                name: "Pago");

            migrationBuilder.DropTable(
                name: "PlazaEstacionamiento");

            migrationBuilder.DropTable(
                name: "TrabajaEn");

            migrationBuilder.DropTable(
                name: "AceptaMetodoPago");

            migrationBuilder.DropTable(
                name: "MetodoPago");
        }
    }
}
