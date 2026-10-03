using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using HotdropBackend.Data;

#nullable disable

namespace HotdropBackend.Data.Migrations;

[DbContext(typeof(HotdropDbContext))]
[Migration("20260928170000_AddAnalyticsIdentityAndPresence")]
public partial class AddAnalyticsIdentityAndPresence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Persist the spec §164 pseudonymous identity (already sent by the
        // client queue format) and the client event clock on every raw event.
        migrationBuilder.AddColumn<string>(
            name: "PseudonymousId",
            table: "AnalyticsEvents",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ClientTimestamp",
            table: "AnalyticsEvents",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_AnalyticsEvents_PseudonymousId",
            table: "AnalyticsEvents",
            column: "PseudonymousId");

        migrationBuilder.CreateIndex(
            name: "IX_AnalyticsEvents_PseudonymousId_Timestamp",
            table: "AnalyticsEvents",
            columns: new[] { "PseudonymousId", "Timestamp" });

        // Per-pseudonymous-player daily presence rollup: survives the 30-day
        // raw window inside the 180-day aggregate retention so D1/D7/D30
        // retention stays computable per user.
        migrationBuilder.CreateTable(
            name: "AnalyticsDailyPresence",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                PseudonymousId = table.Column<string>(type: "text", nullable: false),
                Cohort = table.Column<string>(type: "text", nullable: false),
                FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AnalyticsDailyPresence", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AnalyticsDailyPresence_EventDate_PseudonymousId",
            table: "AnalyticsDailyPresence",
            columns: new[] { "EventDate", "PseudonymousId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AnalyticsDailyPresence_PseudonymousId_EventDate",
            table: "AnalyticsDailyPresence",
            columns: new[] { "PseudonymousId", "EventDate" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AnalyticsEvents_PseudonymousId_Timestamp",
            table: "AnalyticsEvents");

        migrationBuilder.DropIndex(
            name: "IX_AnalyticsEvents_PseudonymousId",
            table: "AnalyticsEvents");

        migrationBuilder.DropTable(
            name: "AnalyticsDailyPresence");

        migrationBuilder.DropColumn(
            name: "ClientTimestamp",
            table: "AnalyticsEvents");

        migrationBuilder.DropColumn(
            name: "PseudonymousId",
            table: "AnalyticsEvents");
    }
}



