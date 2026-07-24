using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutVent.CorePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatesNotification1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clean up any invalid StoreId references first
            migrationBuilder.Sql("UPDATE \"Notifications\" SET \"StoreId\" = NULL WHERE \"StoreId\" NOT IN (SELECT \"Id\" FROM \"Stores\");");

            // Now add the foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Stores_StoreId",
                table: "Notifications",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Stores_StoreId",
                table: "Notifications");
        }
    }
}
