using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace estacionamientos.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Conductores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Documento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conductores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Playas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Provincia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Ciudad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TipoPiso = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ValoracionPromedio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    LlaveRequerida = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehiculos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Patente = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Marca = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Modelo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehiculos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plazas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayaEstacionamientoId = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Nivel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Sector = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    EsCubierta = table.Column<bool>(type: "boolean", nullable: false),
                    EsReservada = table.Column<bool>(type: "boolean", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plazas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plazas_Playas_PlayaEstacionamientoId",
                        column: x => x.PlayaEstacionamientoId,
                        principalTable: "Playas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Servicios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayaEstacionamientoId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servicios_Playas_PlayaEstacionamientoId",
                        column: x => x.PlayaEstacionamientoId,
                        principalTable: "Playas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tarifarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayaEstacionamientoId = table.Column<int>(type: "integer", nullable: false),
                    Clasificacion = table.Column<int>(type: "integer", nullable: false),
                    MontoHora = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    FraccionMin = table.Column<int>(type: "integer", nullable: false),
                    MontoFraccion = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tarifarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tarifarios_Playas_PlayaEstacionamientoId",
                        column: x => x.PlayaEstacionamientoId,
                        principalTable: "Playas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ocupaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlazaEstacionamientoId = table.Column<int>(type: "integer", nullable: false),
                    VehiculoId = table.Column<int>(type: "integer", nullable: false),
                    ConductorId = table.Column<int>(type: "integer", nullable: true),
                    HoraEntrada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HoraSalida = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImporteCalculado = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ocupaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ocupaciones_Conductores_ConductorId",
                        column: x => x.ConductorId,
                        principalTable: "Conductores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Ocupaciones_Plazas_PlazaEstacionamientoId",
                        column: x => x.PlazaEstacionamientoId,
                        principalTable: "Plazas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ocupaciones_Vehiculos_VehiculoId",
                        column: x => x.VehiculoId,
                        principalTable: "Vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OcupacionId = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Metodo = table.Column<int>(type: "integer", nullable: false),
                    Autorizacion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pagos_Ocupaciones_OcupacionId",
                        column: x => x.OcupacionId,
                        principalTable: "Ocupaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ocupaciones_ConductorId",
                table: "Ocupaciones",
                column: "ConductorId");

            migrationBuilder.CreateIndex(
                name: "IX_Ocupaciones_PlazaEstacionamientoId",
                table: "Ocupaciones",
                column: "PlazaEstacionamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ocupaciones_VehiculoId",
                table: "Ocupaciones",
                column: "VehiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_OcupacionId",
                table: "Pagos",
                column: "OcupacionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plazas_PlayaEstacionamientoId_Codigo",
                table: "Plazas",
                columns: new[] { "PlayaEstacionamientoId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Servicios_PlayaEstacionamientoId",
                table: "Servicios",
                column: "PlayaEstacionamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_Tarifarios_PlayaEstacionamientoId",
                table: "Tarifarios",
                column: "PlayaEstacionamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_Patente",
                table: "Vehiculos",
                column: "Patente",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "Servicios");

            migrationBuilder.DropTable(
                name: "Tarifarios");

            migrationBuilder.DropTable(
                name: "Ocupaciones");

            migrationBuilder.DropTable(
                name: "Conductores");

            migrationBuilder.DropTable(
                name: "Plazas");

            migrationBuilder.DropTable(
                name: "Vehiculos");

            migrationBuilder.DropTable(
                name: "Playas");
        }
    }
}
