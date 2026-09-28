using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommerceManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceIntegrationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketplaceIntegrationStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    IsConnected = table.Column<bool>(type: "bit", nullable: false),
                    LastSuccessfulSyncAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastFailedSyncAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastOrderSyncAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastReturnSyncAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastProductSyncAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastInventorySyncAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceIntegrationStatuses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceIntegrationStatuses_Channel",
                table: "MarketplaceIntegrationStatuses",
                column: "Channel",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketplaceIntegrationStatuses");
        }
    }
}
