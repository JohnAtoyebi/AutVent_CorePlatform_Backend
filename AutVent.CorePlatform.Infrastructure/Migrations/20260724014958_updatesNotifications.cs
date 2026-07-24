using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutVent.CorePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatesNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the column as nullable first (no foreign key yet)
            migrationBuilder.AddColumn<long>(
                name: "StoreId",
                table: "Notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_StoreId",
                table: "Notifications",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_StoreId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "StoreId", "IsRead" });

            // Foreign key will be added by a follow-up migration after data cleanup
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_StoreId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_StoreId_IsRead",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Notifications");
        }
    }
}
