using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretParty.Migrations
{
    /// <inheritdoc />
    public partial class Trazas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Trazas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Peticion = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Operacion = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Servicio = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Resultado = table.Column<int>(type: "INTEGER", nullable: false),
                    Detalle = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    DuracionMs = table.Column<long>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trazas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trazas_Fecha",
                table: "Trazas",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Trazas_Peticion",
                table: "Trazas",
                column: "Peticion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Trazas");
        }
    }
}
