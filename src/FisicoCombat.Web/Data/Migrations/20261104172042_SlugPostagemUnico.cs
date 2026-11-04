using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FisicoCombat.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SlugPostagemUnico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Postagens_Slug",
                table: "Postagens",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Postagens_Slug",
                table: "Postagens");
        }
    }
}
