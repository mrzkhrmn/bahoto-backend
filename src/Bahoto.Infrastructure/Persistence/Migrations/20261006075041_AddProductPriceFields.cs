using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bahoto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPriceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastPriceDate",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPriceDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Products");
        }
    }
}
