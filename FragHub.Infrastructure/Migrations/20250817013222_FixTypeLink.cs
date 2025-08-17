using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FragHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixTypeLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlatformUsers_GamePlatforms_GamePlatformId",
                table: "PlatformUsers");

            migrationBuilder.DropIndex(
                name: "IX_PlatformUsers_GamePlatformId",
                table: "PlatformUsers");

            migrationBuilder.RenameColumn(
                name: "GamePlatformId",
                table: "PlatformUsers",
                newName: "GamePlatformType");

            migrationBuilder.CreateIndex(
                name: "IX_GamePlatforms_Type",
                table: "GamePlatforms",
                column: "Type",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GamePlatforms_Type",
                table: "GamePlatforms");

            migrationBuilder.RenameColumn(
                name: "GamePlatformType",
                table: "PlatformUsers",
                newName: "GamePlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUsers_GamePlatformId",
                table: "PlatformUsers",
                column: "GamePlatformId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformUsers_GamePlatforms_GamePlatformId",
                table: "PlatformUsers",
                column: "GamePlatformId",
                principalTable: "GamePlatforms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
