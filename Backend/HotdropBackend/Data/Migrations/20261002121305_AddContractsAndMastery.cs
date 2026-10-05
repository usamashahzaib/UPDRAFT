using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HotdropBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContractsAndMastery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContractGoalInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogVersion = table.Column<string>(type: "text", nullable: false),
                    TemplateId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Progress = table.Column<int>(type: "integer", nullable: false),
                    Target = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractGoalInstances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlightSealGrants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceKey = table.Column<string>(type: "text", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightSealGrants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasteryProgresses",
                columns: table => new
                {
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackId = table.Column<string>(type: "text", nullable: false),
                    WeaponId = table.Column<string>(type: "text", nullable: false),
                    Progress = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasteryProgresses", x => new { x.PlayerId, x.TrackId, x.WeaponId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractGoalInstances_PlayerId_Status",
                table: "ContractGoalInstances",
                columns: new[] { "PlayerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FlightSealGrants_GoalInstanceId",
                table: "FlightSealGrants",
                column: "GoalInstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlightSealGrants_PlayerId_SourceKey",
                table: "FlightSealGrants",
                columns: new[] { "PlayerId", "SourceKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractGoalInstances");

            migrationBuilder.DropTable(
                name: "FlightSealGrants");

            migrationBuilder.DropTable(
                name: "MasteryProgresses");
        }
    }
}
