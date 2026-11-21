using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedPublications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PublicacionGuardada",
                columns: table => new
                {
                    id_guardado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    id_publicacion = table.Column<int>(type: "int", nullable: false),
                    fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicacionGuardada", x => x.id_guardado);
                    table.ForeignKey(
                        name: "FK_PublicacionGuardada_Publicaciones_id_publicacion",
                        column: x => x.id_publicacion,
                        principalTable: "Publicaciones",
                        principalColumn: "id_publicacion",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PublicacionGuardada_Usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "Usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicacionGuardada_id_publicacion",
                table: "PublicacionGuardada",
                column: "id_publicacion");

            migrationBuilder.CreateIndex(
                name: "IX_PublicacionGuardada_id_usuario_id_publicacion",
                table: "PublicacionGuardada",
                columns: new[] { "id_usuario", "id_publicacion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicacionGuardada");
        }
    }
}
