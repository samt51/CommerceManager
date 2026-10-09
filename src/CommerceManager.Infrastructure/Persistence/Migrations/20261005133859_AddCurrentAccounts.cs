using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommerceManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CurrentAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    TaxNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurrentAccountEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentAccountEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrentAccountEntries_CurrentAccounts_CurrentAccountId",
                        column: x => x.CurrentAccountId,
                        principalTable: "CurrentAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAccountEntries_CurrentAccountId",
                table: "CurrentAccountEntries",
                column: "CurrentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentAccounts_Name",
                table: "CurrentAccounts",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrentAccountEntries");

            migrationBuilder.DropTable(
                name: "CurrentAccounts");
        }
    }
}
