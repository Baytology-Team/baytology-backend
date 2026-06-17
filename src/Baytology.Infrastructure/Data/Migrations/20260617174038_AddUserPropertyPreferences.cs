using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baytology.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPropertyPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPropertyPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PropertyType = table.Column<int>(type: "int", nullable: true),
                    ListingType = table.Column<int>(type: "int", nullable: true),
                    MinPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MinArea = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxArea = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MinBedrooms = table.Column<int>(type: "int", nullable: true),
                    MaxBedrooms = table.Column<int>(type: "int", nullable: true),
                    MinBathrooms = table.Column<int>(type: "int", nullable: true),
                    MaxBathrooms = table.Column<int>(type: "int", nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HasParking = table.Column<bool>(type: "bit", nullable: true),
                    HasPool = table.Column<bool>(type: "bit", nullable: true),
                    HasGym = table.Column<bool>(type: "bit", nullable: true),
                    HasElevator = table.Column<bool>(type: "bit", nullable: true),
                    HasSecurity = table.Column<bool>(type: "bit", nullable: true),
                    HasBalcony = table.Column<bool>(type: "bit", nullable: true),
                    HasGarden = table.Column<bool>(type: "bit", nullable: true),
                    HasCentralAC = table.Column<bool>(type: "bit", nullable: true),
                    FurnishingStatus = table.Column<int>(type: "int", nullable: true),
                    ViewType = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPropertyPreferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPropertyPreferences_UserId",
                table: "UserPropertyPreferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPropertyPreferences_UserId_IsActive",
                table: "UserPropertyPreferences",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPropertyPreferences");
        }
    }
}
