using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotdropBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceOneActiveContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ContractGoalInstances_PlayerId",
                table: "ContractGoalInstances",
                column: "PlayerId",
                unique: true,
                filter: "\"Status\" = 'active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContractGoalInstances_PlayerId",
                table: "ContractGoalInstances");
        }
    }
}
