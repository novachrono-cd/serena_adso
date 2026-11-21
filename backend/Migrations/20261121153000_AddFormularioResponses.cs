using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using WebApplication1;

#nullable disable

namespace WebApplication1.Migrations;

[DbContext(typeof(SerenaDbContext))]
[Migration("20261121153000_AddFormularioResponses")]
public partial class AddFormularioResponses : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_formulario_usuario_usuarioid_usuario",
            table: "Formulario");
        migrationBuilder.DropIndex(
            name: "IX_formulario_usuarioid_usuario",
            table: "Formulario");
        migrationBuilder.DropColumn(
            name: "usuarioid_usuario",
            table: "Formulario");

        migrationBuilder.AddColumn<string>(
            name: "descripcion",
            table: "Formulario",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: false,
            defaultValue: "");
        migrationBuilder.AddColumn<int>(
            name: "preguntas_count",
            table: "Formulario",
            type: "int",
            nullable: false,
            defaultValue: 1);
        migrationBuilder.AddColumn<string>(
            name: "tiempo_estimado",
            table: "Formulario",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "3 minutos");
        migrationBuilder.AddColumn<string>(
            name: "id_comunidad",
            table: "Formulario",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Formulario_id_usuario",
            table: "Formulario",
            column: "id_usuario");
        migrationBuilder.AddForeignKey(
            name: "FK_Formulario_Usuario_id_usuario",
            table: "Formulario",
            column: "id_usuario",
            principalTable: "Usuario",
            principalColumn: "id_usuario",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.CreateTable(
            name: "FormularioRespuesta",
            columns: table => new
            {
                id_respuesta = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                id_formulario = table.Column<int>(type: "int", nullable: false),
                id_usuario = table.Column<int>(type: "int", nullable: false),
                respuesta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                fecha_respuesta = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FormularioRespuesta", x => x.id_respuesta);
                table.ForeignKey(
                    name: "FK_FormularioRespuesta_Formulario_id_formulario",
                    column: x => x.id_formulario,
                    principalTable: "Formulario",
                    principalColumn: "id_formulario",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_FormularioRespuesta_Usuario_id_usuario",
                    column: x => x.id_usuario,
                    principalTable: "Usuario",
                    principalColumn: "id_usuario",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FormularioRespuesta_id_formulario_id_usuario",
            table: "FormularioRespuesta",
            columns: new[] { "id_formulario", "id_usuario" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_FormularioRespuesta_id_usuario",
            table: "FormularioRespuesta",
            column: "id_usuario");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FormularioRespuesta");
        migrationBuilder.DropForeignKey(
            name: "FK_Formulario_Usuario_id_usuario",
            table: "Formulario");
        migrationBuilder.DropIndex(
            name: "IX_Formulario_id_usuario",
            table: "Formulario");
        migrationBuilder.DropColumn(name: "descripcion", table: "Formulario");
        migrationBuilder.DropColumn(name: "preguntas_count", table: "Formulario");
        migrationBuilder.DropColumn(name: "tiempo_estimado", table: "Formulario");
        migrationBuilder.DropColumn(name: "id_comunidad", table: "Formulario");

        migrationBuilder.AddColumn<int>(
            name: "usuarioid_usuario",
            table: "Formulario",
            type: "int",
            nullable: true);
        migrationBuilder.CreateIndex(
            name: "IX_formulario_usuarioid_usuario",
            table: "Formulario",
            column: "usuarioid_usuario");
        migrationBuilder.AddForeignKey(
            name: "FK_formulario_usuario_usuarioid_usuario",
            table: "Formulario",
            column: "usuarioid_usuario",
            principalTable: "Usuario",
            principalColumn: "id_usuario");
    }
}
