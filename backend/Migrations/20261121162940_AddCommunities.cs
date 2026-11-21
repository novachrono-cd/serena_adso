using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comunidad",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    centro = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    sub_nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    nombre_completo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    icono = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    es_oficial = table.Column<bool>(type: "bit", nullable: false),
                    creado_por_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comunidad", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Disponibilidad",
                columns: table => new
                {
                    id_disponibilidad = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    id_rol = table.Column<int>(type: "int", nullable: false),
                    dia_semana = table.Column<byte>(type: "tinyint", nullable: false),
                    hora_inicio = table.Column<TimeSpan>(type: "time", nullable: false),
                    hora_fin = table.Column<TimeSpan>(type: "time", nullable: false),
                    estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disponibilidad", x => x.id_disponibilidad);
                    table.ForeignKey(
                        name: "FK_Disponibilidad_Rol_id_rol",
                        column: x => x.id_rol,
                        principalTable: "Rol",
                        principalColumn: "id_rol",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Disponibilidad_Usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "Usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ficha",
                columns: table => new
                {
                    id_ficha = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo_ficha = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    programa = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    centro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    jornada = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ficha", x => x.id_ficha);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioFicha",
                columns: table => new
                {
                    id_usuario_ficha = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    id_ficha = table.Column<int>(type: "int", nullable: false),
                    fecha_asignacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioFicha", x => x.id_usuario_ficha);
                    table.ForeignKey(
                        name: "FK_UsuarioFicha_Ficha_id_ficha",
                        column: x => x.id_ficha,
                        principalTable: "Ficha",
                        principalColumn: "id_ficha",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioFicha_Usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "Usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Disponibilidad_id_rol",
                table: "Disponibilidad",
                column: "id_rol");

            migrationBuilder.CreateIndex(
                name: "IX_Disponibilidad_id_usuario",
                table: "Disponibilidad",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioFicha_id_ficha",
                table: "UsuarioFicha",
                column: "id_ficha");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioFicha_id_usuario",
                table: "UsuarioFicha",
                column: "id_usuario");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comunidad");

            migrationBuilder.DropTable(
                name: "Disponibilidad");

            migrationBuilder.DropTable(
                name: "UsuarioFicha");

            migrationBuilder.DropTable(
                name: "Ficha");

        }
    }
}
