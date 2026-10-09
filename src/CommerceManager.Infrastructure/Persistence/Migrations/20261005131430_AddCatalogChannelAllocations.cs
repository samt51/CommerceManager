using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommerceManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogChannelAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogChannelAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogChannelAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogChannelAllocations_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogChannelAllocations_CatalogProductId_Channel",
                table: "CatalogChannelAllocations",
                columns: new[] { "CatalogProductId", "Channel" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogChannelAllocations");
        }
    }
}
