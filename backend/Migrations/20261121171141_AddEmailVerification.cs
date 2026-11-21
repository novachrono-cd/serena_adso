using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "email_verificado",
                table: "Usuario",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "CodigoVerificacionCorreo",
                columns: table => new
                {
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    codigo_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    expira_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    proximo_envio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    intentos = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodigoVerificacionCorreo", x => x.id_usuario);
                    table.ForeignKey(
                        name: "FK_CodigoVerificacionCorreo_Usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "Usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodigoVerificacionCorreo");

            migrationBuilder.DropColumn(
                name: "email_verificado",
                table: "Usuario");
        }
    }
}
