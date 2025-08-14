using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace estacionamientos.Migrations
{
    /// <inheritdoc />
    public partial class Init_Usuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    UsuNU = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuNyA = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UsuEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    UsuPswd = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UsuNumTel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.UsuNU);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_UsuEmail",
                table: "Usuario",
                column: "UsuEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Usuario");
        }
    }
}
