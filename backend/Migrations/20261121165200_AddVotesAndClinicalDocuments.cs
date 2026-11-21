using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class AddVotesAndClinicalDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SoporteClinico",
                columns: table => new
                {
                    id_soporte = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    nombre_documento = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    entidad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    fecha_emision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    medico_especialista = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    tipo_archivo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    archivo_nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    archivo_tipo_contenido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    archivo_contenido = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    diagnostico_cie10 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoporteClinico", x => x.id_soporte);
                    table.ForeignKey(
                        name: "FK_SoporteClinico_Usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "Usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VotoPublicacion",
                columns: table => new
                {
                    id_voto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    id_publicacion = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotoPublicacion", x => x.id_voto);
                    table.ForeignKey(
                        name: "FK_VotoPublicacion_Publicaciones_id_publicacion",
                        column: x => x.id_publicacion,
                        principalTable: "Publicaciones",
                        principalColumn: "id_publicacion",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VotoPublicacion_Usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "Usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SoporteClinico_id_usuario",
                table: "SoporteClinico",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_VotoPublicacion_id_publicacion",
                table: "VotoPublicacion",
                column: "id_publicacion");

            migrationBuilder.CreateIndex(
                name: "IX_VotoPublicacion_id_usuario_id_publicacion",
                table: "VotoPublicacion",
                columns: new[] { "id_usuario", "id_publicacion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SoporteClinico");

            migrationBuilder.DropTable(
                name: "VotoPublicacion");
        }
    }
}
