using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotdropBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCosmeticSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BadgeId",
                table: "PlayerAccounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyRigId",
                table: "PlayerAccounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyStyleId",
                table: "PlayerAccounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeaponWrapId",
                table: "PlayerAccounts",
                type: "text",
                nullable: true);

            // Amendment 3: backfill the legacy single value into its correct slot so an
            // existing player keeps the look they already had. Runs on PostgreSQL and SQLite.
            migrationBuilder.Sql(
                "UPDATE \"PlayerAccounts\" SET \"BodyStyleId\" = \"EquippedCosmeticId\" " +
                "WHERE \"BodyStyleId\" IS NULL AND \"EquippedCosmeticId\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BadgeId",
                table: "PlayerAccounts");

            migrationBuilder.DropColumn(
                name: "BodyRigId",
                table: "PlayerAccounts");

            migrationBuilder.DropColumn(
                name: "BodyStyleId",
                table: "PlayerAccounts");

            migrationBuilder.DropColumn(
                name: "WeaponWrapId",
                table: "PlayerAccounts");
        }
    }
}
