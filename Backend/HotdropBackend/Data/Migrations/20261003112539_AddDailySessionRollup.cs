using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HotdropBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDailySessionRollup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalyticsDailySessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PseudonymousId = table.Column<string>(type: "text", nullable: false),
                    Sessions = table.Column<int>(type: "integer", nullable: false),
                    SameDayReturns = table.Column<int>(type: "integer", nullable: false),
                    VoluntarySessions = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsDailySessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsDailySessions_EventDate_PseudonymousId",
                table: "AnalyticsDailySessions",
                columns: new[] { "EventDate", "PseudonymousId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsDailySessions_PseudonymousId_EventDate",
                table: "AnalyticsDailySessions",
                columns: new[] { "PseudonymousId", "EventDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyticsDailySessions");
        }
    }
}
