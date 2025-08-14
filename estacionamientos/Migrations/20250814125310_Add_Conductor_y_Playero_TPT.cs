using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace estacionamientos.Migrations
{
    /// <inheritdoc />
    public partial class Add_Conductor_y_Playero_TPT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "Plazas");

            migrationBuilder.DropTable(
                name: "Vehiculos");

            migrationBuilder.DropTable(
                name: "Playas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Conductores",
                table: "Conductores");

            migrationBuilder.DropColumn(
                name: "Apellido",
                table: "Conductores");

            migrationBuilder.DropColumn(
                name: "Documento",
                table: "Conductores");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Conductores");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "Conductores");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Conductores");

            migrationBuilder.RenameTable(
                name: "Conductores",
                newName: "Conductor");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Conductor",
                newName: "UsuNU");

            migrationBuilder.AlterColumn<int>(
                name: "UsuNU",
                table: "Conductor",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Conductor",
                table: "Conductor",
                column: "UsuNU");

            migrationBuilder.CreateTable(
                name: "Duenio",
                columns: table => new
                {
                    UsuNU = table.Column<int>(type: "integer", nullable: false),
                    DueCuit = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Duenio", x => x.UsuNU);
                    table.ForeignKey(
                        name: "FK_Duenio_Usuario_UsuNU",
                        column: x => x.UsuNU,
                        principalTable: "Usuario",
                        principalColumn: "UsuNU",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Playero",
                columns: table => new
                {
                    UsuNU = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playero", x => x.UsuNU);
                    table.ForeignKey(
                        name: "FK_Playero_Usuario_UsuNU",
                        column: x => x.UsuNU,
                        principalTable: "Usuario",
                        principalColumn: "UsuNU",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Duenio_DueCuit",
                table: "Duenio",
                column: "DueCuit",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Conductor_Usuario_UsuNU",
                table: "Conductor",
                column: "UsuNU",
                principalTable: "Usuario",
                principalColumn: "UsuNU",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conductor_Usuario_UsuNU",
                table: "Conductor");

            migrationBuilder.DropTable(
                name: "Duenio");

            migrationBuilder.DropTable(
                name: "Playero");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Conductor",
                table: "Conductor");

            migrationBuilder.RenameTable(
                name: "Conductor",
                newName: "Conductores");

            migrationBuilder.RenameColumn(
                name: "UsuNU",
                table: "Conductores",
                newName: "Id");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Conductores",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "Apellido",
                table: "Conductores",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Documento",
                table: "Conductores",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Conductores",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "Conductores",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Conductores",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Conductores",
                table: "Conductores",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Playas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ciudad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LlaveRequerida = table.Column<bool>(type: "boolean", nullable: false),
                    Provincia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TipoPiso = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ValoracionPromedio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
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
                    Color = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Marca = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Modelo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Patente = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
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
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EsCubierta = table.Column<bool>(type: "boolean", nullable: false),
                    EsReservada = table.Column<bool>(type: "boolean", nullable: false),
                    Nivel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Sector = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
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
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
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
                    FraccionMin = table.Column<int>(type: "integer", nullable: false),
                    MontoFraccion = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    MontoHora = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
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
                    ConductorId = table.Column<int>(type: "integer", nullable: true),
                    PlazaEstacionamientoId = table.Column<int>(type: "integer", nullable: false),
                    VehiculoId = table.Column<int>(type: "integer", nullable: false),
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
                    Autorizacion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Metodo = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
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
    }
}
