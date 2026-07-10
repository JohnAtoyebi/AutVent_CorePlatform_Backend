using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AutVent.CorePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StaffRanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StaffRange",
                table: "Businesses");

            migrationBuilder.CreateTable(
                name: "StaffRanges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_StaffRanges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffRanges_Name",
                table: "StaffRanges",
                column: "Name",
                unique: true);

            // Seed the four staff ranges so the FK can be satisfied before back-filling.
            migrationBuilder.Sql("""
                INSERT INTO "StaffRanges" ("Name", "IsActive", "IsDeleted", "CreatedBy", "DateCreated")
                VALUES
                    ('1-10',   true, false, 'migration', NOW()),
                    ('11-50',  true, false, 'migration', NOW()),
                    ('51-200', true, false, 'migration', NOW()),
                    ('200+',   true, false, 'migration', NOW());
                """);

            // Add the column as nullable first so existing rows are not immediately invalid.
            migrationBuilder.AddColumn<long>(
                name: "StaffRangeId",
                table: "Businesses",
                type: "bigint",
                nullable: true);

            // Back-fill existing businesses to the first staff range (1-10).
            migrationBuilder.Sql("""
                UPDATE "Businesses"
                SET "StaffRangeId" = (SELECT "Id" FROM "StaffRanges" WHERE "Name" = '1-10' LIMIT 1);
                """);

            // Now make the column NOT NULL.
            migrationBuilder.AlterColumn<long>(
                name: "StaffRangeId",
                table: "Businesses",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_StaffRangeId",
                table: "Businesses",
                column: "StaffRangeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Businesses_StaffRanges_StaffRangeId",
                table: "Businesses",
                column: "StaffRangeId",
                principalTable: "StaffRanges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Businesses_StaffRanges_StaffRangeId",
                table: "Businesses");

            migrationBuilder.DropTable(
                name: "StaffRanges");

            migrationBuilder.DropIndex(
                name: "IX_Businesses_StaffRangeId",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "StaffRangeId",
                table: "Businesses");

            migrationBuilder.AddColumn<string>(
                name: "StaffRange",
                table: "Businesses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
