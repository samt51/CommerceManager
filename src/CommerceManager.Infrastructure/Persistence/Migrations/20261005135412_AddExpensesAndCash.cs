using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CommerceManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpensesAndCash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expenses_ExpenseTypes_ExpenseTypeId",
                        column: x => x.ExpenseTypeId,
                        principalTable: "ExpenseTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpenseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashMovements_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ExpenseTypes",
                columns: new[] { "Id", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("8f1c2a10-6b4e-4a1d-9c11-111111111101"), true, "Benzin & Mazot" },
                    { new Guid("8f1c2a10-6b4e-4a1d-9c11-111111111102"), true, "Araç harçlığı" },
                    { new Guid("8f1c2a10-6b4e-4a1d-9c11-111111111103"), true, "Yemek" },
                    { new Guid("8f1c2a10-6b4e-4a1d-9c11-111111111104"), true, "Kira" },
                    { new Guid("8f1c2a10-6b4e-4a1d-9c11-111111111105"), true, "Elektrik" },
                    { new Guid("8f1c2a10-6b4e-4a1d-9c11-111111111106"), true, "Su" },
                    { new Guid("8f1c2a10-6b4e-4a1d-9c11-111111111107"), true, "Doğalgaz" },
                    { new Guid("8f1c2a10-6b4e-4a1d-9c11-111111111108"), true, "Telefon / internet" },
                    { new Guid("8f1c2a10-6b4e-4a1d-9c11-111111111109"), true, "Kargo" },
                    { new Guid("8f1c2a10-6b4e-4a1d-9c11-111111111110"), true, "Personel" },
                    { new Guid("8f1c2a10-6b4e-4a1d-9c11-111111111111"), true, "Mutfak / market" },
                    { new Guid("8f1c2a10-6b4e-4a1d-9c11-111111111112"), true, "Bakım onarım" },
                    { new Guid("8f1c2a10-6b4e-4a1d-9c11-111111111113"), true, "Vergi / SGK" },
                    { new Guid("8f1c2a10-6b4e-4a1d-9c11-111111111114"), true, "Diğer" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_ExpenseId",
                table: "CashMovements",
                column: "ExpenseId",
                unique: true,
                filter: "[ExpenseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_OccurredAt",
                table: "CashMovements",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseTypeId",
                table: "Expenses",
                column: "ExpenseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_OccurredAt",
                table: "Expenses",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseTypes_Name",
                table: "ExpenseTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashMovements");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "ExpenseTypes");
        }
    }
}
