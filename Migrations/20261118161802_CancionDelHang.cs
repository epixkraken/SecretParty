using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretParty.Migrations
{
    /// <inheritdoc />
    public partial class CancionDelHang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancionArtista",
                table: "Eventos",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancionPortada",
                table: "Eventos",
                type: "TEXT",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancionPreview",
                table: "Eventos",
                type: "TEXT",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancionTitulo",
                table: "Eventos",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancionArtista",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "CancionPortada",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "CancionPreview",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "CancionTitulo",
                table: "Eventos");
        }
    }
}
