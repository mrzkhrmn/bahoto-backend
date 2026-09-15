using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bahoto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OilChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Vehicle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Plate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OilType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    KmChanged = table.Column<int>(type: "integer", nullable: false),
                    NextChangeKm = table.Column<int>(type: "integer", nullable: false),
                    OilFilter = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AirFilter = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FuelFilter = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PolenFilter = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Employee = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OilChanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OilChanges");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
