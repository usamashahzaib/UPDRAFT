using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpdraftBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class QaAuditReceiptsReportsRecoveryDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReportedAlias",
                table: "PlayerReports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReportedSlot",
                table: "PlayerReports",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmailHash",
                table: "PlayerAccounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Alias",
                table: "MatchParticipations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreditsGained",
                table: "MatchParticipations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Slot",
                table: "MatchParticipations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnlockedCsv",
                table: "MatchParticipations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "XpGained",
                table: "MatchParticipations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BotCount",
                table: "AnalyticsEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "BotSubstituted",
                table: "AnalyticsEvents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncentivizedTester",
                table: "AnalyticsEvents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "AccountDeletionRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VerificationExpiresAt",
                table: "AccountDeletionRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationTokenHash",
                table: "AccountDeletionRequests",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAccounts_ContactEmailHash",
                table: "PlayerAccounts",
                column: "ContactEmailHash");

            migrationBuilder.CreateIndex(
                name: "IX_AccountDeletionRequests_ContactEmail_Status",
                table: "AccountDeletionRequests",
                columns: new[] { "ContactEmail", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountDeletionRequests_VerificationTokenHash",
                table: "AccountDeletionRequests",
                column: "VerificationTokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerAccounts_ContactEmailHash",
                table: "PlayerAccounts");

            migrationBuilder.DropIndex(
                name: "IX_AccountDeletionRequests_ContactEmail_Status",
                table: "AccountDeletionRequests");

            migrationBuilder.DropIndex(
                name: "IX_AccountDeletionRequests_VerificationTokenHash",
                table: "AccountDeletionRequests");

            migrationBuilder.DropColumn(
                name: "ReportedAlias",
                table: "PlayerReports");

            migrationBuilder.DropColumn(
                name: "ReportedSlot",
                table: "PlayerReports");

            migrationBuilder.DropColumn(
                name: "ContactEmailHash",
                table: "PlayerAccounts");

            migrationBuilder.DropColumn(
                name: "Alias",
                table: "MatchParticipations");

            migrationBuilder.DropColumn(
                name: "CreditsGained",
                table: "MatchParticipations");

            migrationBuilder.DropColumn(
                name: "Slot",
                table: "MatchParticipations");

            migrationBuilder.DropColumn(
                name: "UnlockedCsv",
                table: "MatchParticipations");

            migrationBuilder.DropColumn(
                name: "XpGained",
                table: "MatchParticipations");

            migrationBuilder.DropColumn(
                name: "BotCount",
                table: "AnalyticsEvents");

            migrationBuilder.DropColumn(
                name: "BotSubstituted",
                table: "AnalyticsEvents");

            migrationBuilder.DropColumn(
                name: "IncentivizedTester",
                table: "AnalyticsEvents");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "AccountDeletionRequests");

            migrationBuilder.DropColumn(
                name: "VerificationExpiresAt",
                table: "AccountDeletionRequests");

            migrationBuilder.DropColumn(
                name: "VerificationTokenHash",
                table: "AccountDeletionRequests");
        }
    }
}
