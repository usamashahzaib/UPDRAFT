using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UpdraftBackend.Data;

#nullable disable

namespace UpdraftBackend.Data.Migrations;

[DbContext(typeof(UpdraftDbContext))]
[Migration("20260928160000_AddGooglePlayIdentityUniqueness")]
public partial class AddGooglePlayIdentityUniqueness : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateIndex(
            name: "IX_PlayerAccounts_LinkedPlatformIdentity",
            table: "PlayerAccounts",
            column: "LinkedPlatformIdentity",
            unique: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropIndex(
            name: "IX_PlayerAccounts_LinkedPlatformIdentity",
            table: "PlayerAccounts");
}
