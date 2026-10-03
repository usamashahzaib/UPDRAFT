using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotdropBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class CorrectLedgerAndEquippedCosmetic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CreditLedgerEntries_Reason_SourceId",
                table: "CreditLedgerEntries");

            migrationBuilder.AddColumn<string>(
                name: "EquippedCosmeticId",
                table: "PlayerAccounts",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PlayerId",
                table: "AccountDeletionRequests",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "AccountDeletionRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "AccountDeletionRequests",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditLedgerEntries_PlayerId_Reason_SourceId",
                table: "CreditLedgerEntries",
                columns: new[] { "PlayerId", "Reason", "SourceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CreditLedgerEntries_PlayerId_Reason_SourceId",
                table: "CreditLedgerEntries");

            migrationBuilder.DropColumn(
                name: "EquippedCosmeticId",
                table: "PlayerAccounts");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "AccountDeletionRequests");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "AccountDeletionRequests");

            migrationBuilder.AlterColumn<Guid>(
                name: "PlayerId",
                table: "AccountDeletionRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditLedgerEntries_Reason_SourceId",
                table: "CreditLedgerEntries",
                columns: new[] { "Reason", "SourceId" },
                unique: true);
        }
    }
}

