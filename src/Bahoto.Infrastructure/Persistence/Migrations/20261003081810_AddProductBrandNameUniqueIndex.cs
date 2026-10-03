using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bahoto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductBrandNameUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Products_Brand_Name",
                table: "Products",
                columns: new[] { "Brand", "Name" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Brand_Name",
                table: "Products");
        }
    }
}
