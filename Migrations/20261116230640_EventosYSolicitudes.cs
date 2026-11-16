using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretParty.Migrations
{
    /// <inheritdoc />
    public partial class EventosYSolicitudes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Eventos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FechaHora = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Zona = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Cupo = table.Column<int>(type: "INTEGER", nullable: false),
                    EsPublico = table.Column<bool>(type: "INTEGER", nullable: false),
                    EsOficial = table.Column<bool>(type: "INTEGER", nullable: false),
                    FotoPortada = table.Column<string>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AnfitrionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eventos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Eventos_Usuarios_AnfitrionId",
                        column: x => x.AnfitrionId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Solicitudes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventoId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Mensaje = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitudes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Eventos_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_AnfitrionId",
                table: "Eventos",
                column: "AnfitrionId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_EventoId_UsuarioId",
                table: "Solicitudes",
                columns: new[] { "EventoId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_UsuarioId",
                table: "Solicitudes",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Solicitudes");

            migrationBuilder.DropTable(
                name: "Eventos");
        }
    }
}
