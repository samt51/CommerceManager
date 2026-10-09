using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommerceManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogProductSaleDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CatalogProducts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "CatalogProducts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "CatalogProducts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "CatalogProducts");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "CatalogProducts");

            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "CatalogProducts");
        }
    }
}
