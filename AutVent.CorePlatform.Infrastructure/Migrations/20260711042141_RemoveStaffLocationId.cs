using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutVent.CorePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStaffLocationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Staff_Stores_LocationId",
                table: "Staff");

            migrationBuilder.DropIndex(
                name: "IX_Staff_LocationId",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Staff");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LocationId",
                table: "Staff",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Staff_LocationId",
                table: "Staff",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Staff_Stores_LocationId",
                table: "Staff",
                column: "LocationId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
