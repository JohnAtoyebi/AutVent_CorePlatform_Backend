using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutVent.CorePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleStaffId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "StaffId",
                table: "Sales",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_StaffId",
                table: "Sales",
                column: "StaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Staff_StaffId",
                table: "Sales",
                column: "StaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Staff_StaffId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_StaffId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "StaffId",
                table: "Sales");
        }
    }
}
