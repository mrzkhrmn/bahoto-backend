using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bahoto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DecoupleCariFromProductsAndDropSuppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Caris_Products_ProductId",
                table: "Caris");

            migrationBuilder.DropTable(
                name: "SupplierEntries");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.CreateTable(
                name: "CariItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CariItems", x => x.Id);
                });

            // Mevcut cari hareketlerindeki ürünleri bağımsız CariItem kayıtlarına taşı
            // (Id aynı bırakılır → ProductId kolonunu CariItemId olarak yeniden adlandırabiliriz)
            migrationBuilder.Sql("""
                INSERT INTO "CariItems" ("Id", "Brand", "Name", "CreatedAt", "UpdatedAt", "DeletedAt")
                SELECT p."Id",
                       COALESCE(p."Brand", ''),
                       COALESCE(p."Name", ''),
                       p."CreatedAt",
                       p."UpdatedAt",
                       p."DeletedAt"
                FROM "Products" p
                WHERE p."Id" IN (SELECT DISTINCT c."ProductId" FROM "Caris" c);
                """);

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "Caris",
                newName: "CariItemId");

            migrationBuilder.RenameIndex(
                name: "IX_Caris_ProductId",
                table: "Caris",
                newName: "IX_Caris_CariItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CariItems_Brand_Name",
                table: "CariItems",
                columns: new[] { "Brand", "Name" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Caris_CariItems_CariItemId",
                table: "Caris",
                column: "CariItemId",
                principalTable: "CariItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Cari için oluşturulmuş markasız ürünleri fiyat tablosundan kaldır
            migrationBuilder.Sql("""
                UPDATE "Products" p
                SET "DeletedAt" = NOW() AT TIME ZONE 'UTC',
                    "UpdatedAt" = NOW() AT TIME ZONE 'UTC'
                WHERE p."Brand" = ''
                  AND p."DeletedAt" IS NULL
                  AND EXISTS (SELECT 1 FROM "CariItems" ci WHERE ci."Id" = p."Id");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Caris_CariItems_CariItemId",
                table: "Caris");

            migrationBuilder.DropTable(
                name: "CariItems");

            migrationBuilder.RenameColumn(
                name: "CariItemId",
                table: "Caris",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_Caris_CariItemId",
                table: "Caris",
                newName: "IX_Caris_ProductId");

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IncomingAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    QuantityUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierEntries_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierEntries_SupplierId",
                table: "SupplierEntries",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Name",
                table: "Suppliers",
                column: "Name",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Caris_Products_ProductId",
                table: "Caris",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
