using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetPurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "proposito",
                table: "CodigoVerificacionCorreo",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "VerificacionCorreo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "proposito",
                table: "CodigoVerificacionCorreo");
        }
    }
}
