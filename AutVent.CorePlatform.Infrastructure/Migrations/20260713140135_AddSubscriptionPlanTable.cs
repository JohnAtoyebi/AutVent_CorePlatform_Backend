using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AutVent.CorePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlanTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Plan",
                table: "BusinessSubscriptions");

            migrationBuilder.AddColumn<long>(
                name: "SubscriptionPlanId",
                table: "BusinessSubscriptions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "SubscriptionPlanDefinitions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Plan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MonthlyPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaxStores = table.Column<int>(type: "integer", nullable: true),
                    MaxStaff = table.Column<int>(type: "integer", nullable: true),
                    MaxProducts = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlanDefinitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSubscriptions_SubscriptionPlanId",
                table: "BusinessSubscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlanDefinitions_Plan",
                table: "SubscriptionPlanDefinitions",
                column: "Plan",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessSubscriptions_SubscriptionPlanDefinitions_Subscript~",
                table: "BusinessSubscriptions",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlanDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessSubscriptions_SubscriptionPlanDefinitions_Subscript~",
                table: "BusinessSubscriptions");

            migrationBuilder.DropTable(
                name: "SubscriptionPlanDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_BusinessSubscriptions_SubscriptionPlanId",
                table: "BusinessSubscriptions");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanId",
                table: "BusinessSubscriptions");

            migrationBuilder.AddColumn<string>(
                name: "Plan",
                table: "BusinessSubscriptions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
