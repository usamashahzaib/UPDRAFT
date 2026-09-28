using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpdraftBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAliasAndInventoryUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Alias",
                table: "PlayerAccounts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_PlayerId_CosmeticId",
                table: "InventoryItems",
                columns: new[] { "PlayerId", "CosmeticId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_PlayerId_CosmeticId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Alias",
                table: "PlayerAccounts");
        }
    }
}
