using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace estacionamientos.Migrations
{
    /// <inheritdoc />
    public partial class Migration_20250925_0945 : Migration
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
                name: "PlayaEstacionamiento",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlyNom = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PlyProv = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PlyProvId = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    PlyCiu = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PlyCiuId = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    PlyDir = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PlyTipoPiso = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PlyValProm = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false, defaultValue: 0m),
                    PlyLlavReq = table.Column<bool>(type: "boolean", nullable: false),
                    PlyLat = table.Column<decimal>(type: "numeric", nullable: true),
                    PlyLon = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayaEstacionamiento", x => x.PlyID);
                });

            migrationBuilder.CreateTable(
                name: "Servicio",
                columns: table => new
                {
                    SerID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SerNom = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SerTipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    SerDesc = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicio", x => x.SerID);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    UsuNU = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuNyA = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UsuEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    UsuPswd = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UsuNumTel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    UsuNomUsu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.UsuNU);
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
                name: "PlazaEstacionamiento",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    PlzNum = table.Column<int>(type: "integer", nullable: false),
                    PlzOcupada = table.Column<bool>(type: "boolean", nullable: false),
                    PlzTecho = table.Column<bool>(type: "boolean", nullable: false),
                    PlzAlt = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    PlzHab = table.Column<bool>(type: "boolean", nullable: false),
                    PlzNombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Piso = table.Column<int>(type: "integer", nullable: true)
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
                name: "ServicioProveido",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    SerID = table.Column<int>(type: "integer", nullable: false),
                    SerProvHab = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicioProveido", x => new { x.PlyID, x.SerID });
                    table.ForeignKey(
                        name: "FK_ServicioProveido_PlayaEstacionamiento_PlyID",
                        column: x => x.PlyID,
                        principalTable: "PlayaEstacionamiento",
                        principalColumn: "PlyID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServicioProveido_Servicio_SerID",
                        column: x => x.SerID,
                        principalTable: "Servicio",
                        principalColumn: "SerID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Administrador",
                columns: table => new
                {
                    UsuNU = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Administrador", x => x.UsuNU);
                    table.ForeignKey(
                        name: "FK_Administrador_Usuario_UsuNU",
                        column: x => x.UsuNU,
                        principalTable: "Usuario",
                        principalColumn: "UsuNU",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Conductor",
                columns: table => new
                {
                    UsuNU = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conductor", x => x.UsuNU);
                    table.ForeignKey(
                        name: "FK_Conductor_Usuario_UsuNU",
                        column: x => x.UsuNU,
                        principalTable: "Usuario",
                        principalColumn: "UsuNU",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "Pago",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    PagNum = table.Column<int>(type: "integer", nullable: false),
                    MepID = table.Column<int>(type: "integer", nullable: false),
                    PagMonto = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    PagFyh = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AceptaMetodoPagoMepID = table.Column<int>(type: "integer", nullable: true),
                    AceptaMetodoPagoPlyID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pago", x => new { x.PlyID, x.PagNum });
                    table.ForeignKey(
                        name: "FK_Pago_AceptaMetodoPago_AceptaMetodoPagoPlyID_AceptaMetodoPag~",
                        columns: x => new { x.AceptaMetodoPagoPlyID, x.AceptaMetodoPagoMepID },
                        principalTable: "AceptaMetodoPago",
                        principalColumns: new[] { "PlyID", "MepID" });
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
                        name: "FK_Pago_PlayaEstacionamiento_PlyID",
                        column: x => x.PlyID,
                        principalTable: "PlayaEstacionamiento",
                        principalColumn: "PlyID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlazaClasificacion",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    PlzNum = table.Column<int>(type: "integer", nullable: false),
                    ClasVehID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlazaClasificacion", x => new { x.PlyID, x.PlzNum, x.ClasVehID });
                    table.ForeignKey(
                        name: "FK_PlazaClasificacion_ClasificacionVehiculo_ClasVehID",
                        column: x => x.ClasVehID,
                        principalTable: "ClasificacionVehiculo",
                        principalColumn: "ClasVehID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlazaClasificacion_PlazaEstacionamiento_PlyID_PlzNum",
                        columns: x => new { x.PlyID, x.PlzNum },
                        principalTable: "PlazaEstacionamiento",
                        principalColumns: new[] { "PlyID", "PlzNum" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TarifaServicio",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    SerID = table.Column<int>(type: "integer", nullable: false),
                    ClasVehID = table.Column<int>(type: "integer", nullable: false),
                    TasFecIni = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TasFecFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TasMonto = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarifaServicio", x => new { x.PlyID, x.SerID, x.ClasVehID, x.TasFecIni });
                    table.ForeignKey(
                        name: "FK_TarifaServicio_ClasificacionVehiculo_ClasVehID",
                        column: x => x.ClasVehID,
                        principalTable: "ClasificacionVehiculo",
                        principalColumn: "ClasVehID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TarifaServicio_ServicioProveido_PlyID_SerID",
                        columns: x => new { x.PlyID, x.SerID },
                        principalTable: "ServicioProveido",
                        principalColumns: new[] { "PlyID", "SerID" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Abonado",
                columns: table => new
                {
                    AboDNI = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    AboNom = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ConNU = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Abonado", x => x.AboDNI);
                    table.ForeignKey(
                        name: "FK_Abonado_Conductor_ConNU",
                        column: x => x.ConNU,
                        principalTable: "Conductor",
                        principalColumn: "UsuNU",
                        onDelete: ReferentialAction.SetNull);
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
                name: "TrabajaEn",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    PlaNU = table.Column<int>(type: "integer", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    TrabEnActual = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FechaFin = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrabajaEn", x => new { x.PlyID, x.PlaNU, x.FechaInicio });
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
                name: "Ocupacion",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    PlzNum = table.Column<int>(type: "integer", nullable: false),
                    VehPtnt = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    OcufFyhIni = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OcufFyhFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OcuLlavDej = table.Column<bool>(type: "boolean", nullable: false),
                    PagNum = table.Column<int>(type: "integer", nullable: true),
                    PagoPagNum = table.Column<int>(type: "integer", nullable: true),
                    PagoPlyID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ocupacion", x => new { x.PlyID, x.PlzNum, x.VehPtnt, x.OcufFyhIni });
                    table.ForeignKey(
                        name: "FK_Ocupacion_Pago_PagoPlyID_PagoPagNum",
                        columns: x => new { x.PagoPlyID, x.PagoPagNum },
                        principalTable: "Pago",
                        principalColumns: new[] { "PlyID", "PagNum" });
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

            migrationBuilder.CreateTable(
                name: "ServicioExtraRealizado",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    SerID = table.Column<int>(type: "integer", nullable: false),
                    VehPtnt = table.Column<string>(type: "character varying(10)", nullable: false),
                    ServExFyHIni = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ServExFyHFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ServExComp = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PagNum = table.Column<int>(type: "integer", nullable: true),
                    PagoPagNum = table.Column<int>(type: "integer", nullable: true),
                    PagoPlyID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicioExtraRealizado", x => new { x.PlyID, x.SerID, x.VehPtnt, x.ServExFyHIni });
                    table.ForeignKey(
                        name: "FK_ServicioExtraRealizado_Pago_PagoPlyID_PagoPagNum",
                        columns: x => new { x.PagoPlyID, x.PagoPagNum },
                        principalTable: "Pago",
                        principalColumns: new[] { "PlyID", "PagNum" });
                    table.ForeignKey(
                        name: "FK_ServicioExtraRealizado_Pago_PlyID_PagNum",
                        columns: x => new { x.PlyID, x.PagNum },
                        principalTable: "Pago",
                        principalColumns: new[] { "PlyID", "PagNum" },
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServicioExtraRealizado_ServicioProveido_PlyID_SerID",
                        columns: x => new { x.PlyID, x.SerID },
                        principalTable: "ServicioProveido",
                        principalColumns: new[] { "PlyID", "SerID" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServicioExtraRealizado_Vehiculo_VehPtnt",
                        column: x => x.VehPtnt,
                        principalTable: "Vehiculo",
                        principalColumn: "VehPtnt",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Abono",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    PlzNum = table.Column<int>(type: "integer", nullable: false),
                    AboFyhIni = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AboFyhFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AboDNI = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    PagNum = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Abono", x => new { x.PlyID, x.PlzNum, x.AboFyhIni });
                    table.ForeignKey(
                        name: "FK_Abono_Abonado_AboDNI",
                        column: x => x.AboDNI,
                        principalTable: "Abonado",
                        principalColumn: "AboDNI",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Abono_Pago_PlyID_PagNum",
                        columns: x => new { x.PlyID, x.PagNum },
                        principalTable: "Pago",
                        principalColumns: new[] { "PlyID", "PagNum" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Abono_PlazaEstacionamiento_PlyID_PlzNum",
                        columns: x => new { x.PlyID, x.PlzNum },
                        principalTable: "PlazaEstacionamiento",
                        principalColumns: new[] { "PlyID", "PlzNum" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Turno",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    PlaNU = table.Column<int>(type: "integer", nullable: false),
                    TurFyhIni = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    TurFyhFin = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    TrabFyhIni = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    TurApertCaja = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    TurCierrCaja = table.Column<decimal>(type: "numeric(12,2)", nullable: true)
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
                        name: "FK_Turno_TrabajaEn_PlyID_PlaNU_TrabFyhIni",
                        columns: x => new { x.PlyID, x.PlaNU, x.TrabFyhIni },
                        principalTable: "TrabajaEn",
                        principalColumns: new[] { "PlyID", "PlaNU", "FechaInicio" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehiculoAbonado",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false),
                    PlzNum = table.Column<int>(type: "integer", nullable: false),
                    AboFyhIni = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VehPtnt = table.Column<string>(type: "character varying(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehiculoAbonado", x => new { x.PlyID, x.PlzNum, x.AboFyhIni, x.VehPtnt });
                    table.ForeignKey(
                        name: "FK_VehiculoAbonado_Abono_PlyID_PlzNum_AboFyhIni",
                        columns: x => new { x.PlyID, x.PlzNum, x.AboFyhIni },
                        principalTable: "Abono",
                        principalColumns: new[] { "PlyID", "PlzNum", "AboFyhIni" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VehiculoAbonado_Vehiculo_VehPtnt",
                        column: x => x.VehPtnt,
                        principalTable: "Vehiculo",
                        principalColumn: "VehPtnt",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ClasificacionDias",
                columns: new[] { "ClaDiasID", "ClaDiasDesc", "ClaDiasTipo" },
                values: new object[,]
                {
                    { 1, "Lunes a Viernes", "Hábil" },
                    { 2, "Sábado y Domingo", "Fin de semana" },
                    { 3, "Feriados no laborables", "Feriado" }
                });

            migrationBuilder.InsertData(
                table: "ClasificacionVehiculo",
                columns: new[] { "ClasVehID", "ClasVehDesc", "ClasVehTipo" },
                values: new object[,]
                {
                    { 1, "Vehículo de pasajeros", "Automóvil" },
                    { 2, "Vehículo utilitario", "Camioneta" },
                    { 3, "Vehículo de carga", "Camión" },
                    { 4, "Vehículo de dos ruedas", "Motocicleta" }
                });

            migrationBuilder.InsertData(
                table: "MetodoPago",
                columns: new[] { "MepID", "MepDesc", "MepNom" },
                values: new object[,]
                {
                    { 1, "Pago en efectivo", "Efectivo" },
                    { 2, "Pago con tarjeta de crédito", "Tarjeta de crédito" },
                    { 3, "Pago con tarjeta de débito", "Tarjeta de débito" },
                    { 4, "Pago mediante transferencia bancaria", "Transferencia bancaria" }
                });

            migrationBuilder.InsertData(
                table: "Servicio",
                columns: new[] { "SerID", "SerDesc", "SerNom", "SerTipo" },
                values: new object[,]
                {
                    { 1, "Lavado exterior e interior del vehículo", "Lavado de vehículo", "ServicioExtra" },
                    { 2, "Revisión y mantenimiento mecánico del vehículo", "Mantenimiento de vehículo", "ServicioExtra" },
                    { 3, "Carga de combustible en el vehículo", "Carga de combustible", "ServicioExtra" },
                    { 4, "Revisión técnica del vehículo para verificar su estado", "Revisión técnica", "ServicioExtra" },
                    { 5, "Servicio de estacionamiento por 1 hora en playa", "Estacionamiento por 1 Hora", "Estacionamiento" },
                    { 6, "Servicio de estacionamiento por 6 horas en playa", "Estacionamiento por 6 Horas", "Estacionamiento" },
                    { 7, "Servicio de estacionamiento por 1 día en playa", "Estacionamiento por 1 Día", "Estacionamiento" },
                    { 8, "Servicio de estacionamiento por 1 semana en playa", "Estacionamiento por 1 Semana", "Estacionamiento" },
                    { 9, "Servicio de estacionamiento por 1 mes en playa", "Estacionamiento por 1 Mes", "Estacionamiento" }
                });

            migrationBuilder.InsertData(
                table: "Usuario",
                columns: new[] { "UsuNU", "UsuEmail", "UsuNomUsu", "UsuNumTel", "UsuNyA", "UsuPswd" },
                values: new object[,]
                {
                    { 1, "castromauricionicolas@hotmail.com", "MauriCastro", "1234567890", "Mauricio Nicolás Castro", "12345678" },
                    { 2, "brizuelajoelelian@gmail.com", "YoelBrizuela", "0987654321", "Yoel Brizuela Silvestri", "12345678" },
                    { 3, "nadineperaltaruiz@gmail.com", "NadinePeralta", "1122334455", "Nadine Andrea Peralta Ruiz", "12345678" },
                    { 4, "mateobeneyto@gmail.com", "MateoBeneyto", "5566778899", "Mateo Beneyto", "12345678" },
                    { 5, "ivan.nikcevich@hotmail.com", "IvanNikcevich", "2233445566", "Iván Josué Nikcevich", "12345678" },
                    { 6, "adri.nikce30@gmail.com", "AdrianoNikcevich", "6677889900", "Adriano Nikcevich", "12345678" },
                    { 7, "solana.livio1976@gmail.com", "SolanaLivio", "3344556677", "Solana Livio", "12345678" },
                    { 8, "obregon.elias@gmail.com", "EliasObregon", "7788990011", "Elías Obregón", "12345678" }
                });

            migrationBuilder.InsertData(
                table: "Administrador",
                column: "UsuNU",
                values: new object[]
                {
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                    8
                });

            migrationBuilder.CreateIndex(
                name: "IX_Abonado_ConNU",
                table: "Abonado",
                column: "ConNU");

            migrationBuilder.CreateIndex(
                name: "IX_Abono_AboDNI",
                table: "Abono",
                column: "AboDNI");

            migrationBuilder.CreateIndex(
                name: "IX_Abono_PlyID_PagNum",
                table: "Abono",
                columns: new[] { "PlyID", "PagNum" });

            migrationBuilder.CreateIndex(
                name: "IX_AceptaMetodoPago_MepID",
                table: "AceptaMetodoPago",
                column: "MepID");

            migrationBuilder.CreateIndex(
                name: "IX_AdministraPlaya_PlyID",
                table: "AdministraPlaya",
                column: "PlyID");

            migrationBuilder.CreateIndex(
                name: "IX_ClasificacionDias_ClaDiasTipo",
                table: "ClasificacionDias",
                column: "ClaDiasTipo",
                unique: true);

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
                name: "IX_Duenio_DueCuit",
                table: "Duenio",
                column: "DueCuit",
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
                name: "IX_Ocupacion_PagoPlyID_PagoPagNum",
                table: "Ocupacion",
                columns: new[] { "PagoPlyID", "PagoPagNum" });

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
                name: "IX_Pago_AceptaMetodoPagoPlyID_AceptaMetodoPagoMepID",
                table: "Pago",
                columns: new[] { "AceptaMetodoPagoPlyID", "AceptaMetodoPagoMepID" });

            migrationBuilder.CreateIndex(
                name: "IX_Pago_MepID",
                table: "Pago",
                column: "MepID");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_PlyID_MepID",
                table: "Pago",
                columns: new[] { "PlyID", "MepID" });

            migrationBuilder.CreateIndex(
                name: "IX_Pago_PlyID_PagFyh",
                table: "Pago",
                columns: new[] { "PlyID", "PagFyh" });

            migrationBuilder.CreateIndex(
                name: "IX_PlazaClasificacion_ClasVehID",
                table: "PlazaClasificacion",
                column: "ClasVehID");

            migrationBuilder.CreateIndex(
                name: "IX_Servicio_SerNom",
                table: "Servicio",
                column: "SerNom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicioExtraRealizado_PagoPlyID_PagoPagNum",
                table: "ServicioExtraRealizado",
                columns: new[] { "PagoPlyID", "PagoPagNum" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicioExtraRealizado_PlyID_PagNum",
                table: "ServicioExtraRealizado",
                columns: new[] { "PlyID", "PagNum" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicioExtraRealizado_PlyID_SerID_ServExFyHIni",
                table: "ServicioExtraRealizado",
                columns: new[] { "PlyID", "SerID", "ServExFyHIni" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicioExtraRealizado_VehPtnt",
                table: "ServicioExtraRealizado",
                column: "VehPtnt");

            migrationBuilder.CreateIndex(
                name: "IX_ServicioProveido_SerID",
                table: "ServicioProveido",
                column: "SerID");

            migrationBuilder.CreateIndex(
                name: "IX_TarifaServicio_ClasVehID",
                table: "TarifaServicio",
                column: "ClasVehID");

            migrationBuilder.CreateIndex(
                name: "IX_TarifaServicio_PlyID_SerID_ClasVehID_TasFecIni",
                table: "TarifaServicio",
                columns: new[] { "PlyID", "SerID", "ClasVehID", "TasFecIni" });

            migrationBuilder.CreateIndex(
                name: "IX_TrabajaEn_PlaNU",
                table: "TrabajaEn",
                column: "PlaNU");

            migrationBuilder.CreateIndex(
                name: "IX_TrabajaEn_PlyID_PlaNU",
                table: "TrabajaEn",
                columns: new[] { "PlyID", "PlaNU" },
                unique: true,
                filter: "\"FechaFin\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Turno_PlaNU",
                table: "Turno",
                column: "PlaNU");

            migrationBuilder.CreateIndex(
                name: "IX_Turno_PlyID_PlaNU_TrabFyhIni",
                table: "Turno",
                columns: new[] { "PlyID", "PlaNU", "TrabFyhIni" });

            migrationBuilder.CreateIndex(
                name: "IX_Turno_PlyID_TurFyhIni",
                table: "Turno",
                columns: new[] { "PlyID", "TurFyhIni" });

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_UsuEmail",
                table: "Usuario",
                column: "UsuEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_UsuNomUsu",
                table: "Usuario",
                column: "UsuNomUsu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Valoracion_ConNU",
                table: "Valoracion",
                column: "ConNU");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculo_ClasVehID",
                table: "Vehiculo",
                column: "ClasVehID");

            migrationBuilder.CreateIndex(
                name: "IX_VehiculoAbonado_VehPtnt",
                table: "VehiculoAbonado",
                column: "VehPtnt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Administrador");

            migrationBuilder.DropTable(
                name: "AdministraPlaya");

            migrationBuilder.DropTable(
                name: "Conduce");

            migrationBuilder.DropTable(
                name: "Horario");

            migrationBuilder.DropTable(
                name: "Ocupacion");

            migrationBuilder.DropTable(
                name: "PlazaClasificacion");

            migrationBuilder.DropTable(
                name: "ServicioExtraRealizado");

            migrationBuilder.DropTable(
                name: "TarifaServicio");

            migrationBuilder.DropTable(
                name: "Turno");

            migrationBuilder.DropTable(
                name: "UbicacionFavorita");

            migrationBuilder.DropTable(
                name: "Valoracion");

            migrationBuilder.DropTable(
                name: "VehiculoAbonado");

            migrationBuilder.DropTable(
                name: "Duenio");

            migrationBuilder.DropTable(
                name: "ClasificacionDias");

            migrationBuilder.DropTable(
                name: "ServicioProveido");

            migrationBuilder.DropTable(
                name: "TrabajaEn");

            migrationBuilder.DropTable(
                name: "Abono");

            migrationBuilder.DropTable(
                name: "Vehiculo");

            migrationBuilder.DropTable(
                name: "Servicio");

            migrationBuilder.DropTable(
                name: "Playero");

            migrationBuilder.DropTable(
                name: "Abonado");

            migrationBuilder.DropTable(
                name: "Pago");

            migrationBuilder.DropTable(
                name: "PlazaEstacionamiento");

            migrationBuilder.DropTable(
                name: "ClasificacionVehiculo");

            migrationBuilder.DropTable(
                name: "Conductor");

            migrationBuilder.DropTable(
                name: "AceptaMetodoPago");

            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropTable(
                name: "MetodoPago");

            migrationBuilder.DropTable(
                name: "PlayaEstacionamiento");
        }
    }
}
