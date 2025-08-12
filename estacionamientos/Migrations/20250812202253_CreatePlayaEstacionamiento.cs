using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace estacionamientos.Migrations
{
    /// <inheritdoc />
    public partial class CreatePlayaEstacionamiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayasEstacionamiento",
                columns: table => new
                {
                    PlyID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlyProv = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PlyCiu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PlyDir = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PlyTipoPiso = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PlyValProm = table.Column<decimal>(type: "numeric", nullable: false),
                    PlyLlavReq = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayasEstacionamiento", x => x.PlyID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayasEstacionamiento");
        }
    }
}
