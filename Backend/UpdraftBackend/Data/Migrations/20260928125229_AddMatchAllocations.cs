using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpdraftBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchAllocations",
                columns: table => new
                {
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerId = table.Column<string>(type: "text", nullable: false),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchAllocations", x => x.MatchId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchAllocations_ServerId",
                table: "MatchAllocations",
                column: "ServerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchAllocations");
        }
    }
}
