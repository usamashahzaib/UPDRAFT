using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotdropBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchmakingTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchmakingTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Region = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    EnqueuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Team = table.Column<string>(type: "text", nullable: true),
                    HasBots = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchmakingTickets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchmakingTickets_PartyId",
                table: "MatchmakingTickets",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchmakingTickets_PlayerId",
                table: "MatchmakingTickets",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchmakingTickets_Status",
                table: "MatchmakingTickets",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchmakingTickets");
        }
    }
}

