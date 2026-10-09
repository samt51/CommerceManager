using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommerceManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogBarcodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogBarcodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogBarcodes_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatalogStockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogStockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogStockMovements_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogBarcodes_CatalogProductId",
                table: "CatalogBarcodes",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogBarcodes_Channel_Barcode",
                table: "CatalogBarcodes",
                columns: new[] { "Channel", "Barcode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_CreatedAt",
                table: "CatalogProducts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogStockMovements_CatalogProductId",
                table: "CatalogStockMovements",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogStockMovements_Channel_ReferenceType_ReferenceId_Barcode_Type",
                table: "CatalogStockMovements",
                columns: new[] { "Channel", "ReferenceType", "ReferenceId", "Barcode", "Type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogBarcodes");

            migrationBuilder.DropTable(
                name: "CatalogStockMovements");

            migrationBuilder.DropTable(
                name: "CatalogProducts");
        }
    }
}
