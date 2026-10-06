using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotdropBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchReceiptProgressBounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProfileXpAfter",
                table: "MatchParticipations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfileXpBefore",
                table: "MatchParticipations",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileXpAfter",
                table: "MatchParticipations");

            migrationBuilder.DropColumn(
                name: "ProfileXpBefore",
                table: "MatchParticipations");
        }
    }
}
