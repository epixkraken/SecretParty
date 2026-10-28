using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SecretParty.Migrations
{
    /// <inheritdoc />
    public partial class Reportes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriasReporte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasReporte", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MotivosReporte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CategoriaReporteId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotivosReporte", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MotivosReporte_CategoriasReporte_CategoriaReporteId",
                        column: x => x.CategoriaReporteId,
                        principalTable: "CategoriasReporte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reportes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioReportadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    MotivoReporteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Comentario = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reportes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reportes_MotivosReporte_MotivoReporteId",
                        column: x => x.MotivoReporteId,
                        principalTable: "MotivosReporte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reportes_Usuarios_UsuarioReportadoId",
                        column: x => x.UsuarioReportadoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CategoriasReporte",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 1, "Perfil falso" },
                    { 2, "Comportamiento" },
                    { 3, "Contenido inapropiado" }
                });

            migrationBuilder.InsertData(
                table: "MotivosReporte",
                columns: new[] { "Id", "CategoriaReporteId", "Nombre" },
                values: new object[,]
                {
                    { 1, 1, "Fotos robadas" },
                    { 2, 1, "Se hace pasar por otra persona" },
                    { 3, 1, "Parece menor de edad" },
                    { 4, 2, "Acoso" },
                    { 5, 2, "Lenguaje ofensivo" },
                    { 6, 2, "No se presentó al evento" },
                    { 7, 3, "Fotos explícitas" },
                    { 8, 3, "Spam o publicidad" },
                    { 9, 3, "Venta de sustancias" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MotivosReporte_CategoriaReporteId",
                table: "MotivosReporte",
                column: "CategoriaReporteId");

            migrationBuilder.CreateIndex(
                name: "IX_Reportes_MotivoReporteId",
                table: "Reportes",
                column: "MotivoReporteId");

            migrationBuilder.CreateIndex(
                name: "IX_Reportes_UsuarioReportadoId",
                table: "Reportes",
                column: "UsuarioReportadoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reportes");

            migrationBuilder.DropTable(
                name: "MotivosReporte");

            migrationBuilder.DropTable(
                name: "CategoriasReporte");
        }
    }
}
