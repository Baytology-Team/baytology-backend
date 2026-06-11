using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baytology.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePropertyValidationConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Properties_BusinessRules",
                table: "Properties");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Properties_BusinessRules",
                table: "Properties",
                sql: "[Price] > 0 AND [Price] >= 1000 AND [Price] <= 999999999 AND [Area] > 0 AND [Area] >= 10 AND [Area] <= 100000 AND [Bedrooms] >= 0 AND [Bedrooms] <= 100 AND [Bathrooms] >= 0 AND [Bathrooms] <= 100 AND ([Floor] IS NULL OR ([Floor] >= 0 AND [Floor] <= 999)) AND ([TotalFloors] IS NULL OR ([TotalFloors] > 0 AND [TotalFloors] <= 999)) AND ([Floor] IS NULL OR [TotalFloors] IS NULL OR [Floor] <= [TotalFloors]) AND ([Latitude] IS NULL OR ([Latitude] >= -90 AND [Latitude] <= 90)) AND ([Longitude] IS NULL OR ([Longitude] >= -180 AND [Longitude] <= 180))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Properties_BusinessRules",
                table: "Properties");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Properties_BusinessRules",
                table: "Properties",
                sql: "[Price] > 0 AND [Area] > 0 AND [Bedrooms] >= 0 AND [Bathrooms] >= 0 AND ([Floor] IS NULL OR [Floor] >= 0) AND ([TotalFloors] IS NULL OR [TotalFloors] > 0) AND ([Floor] IS NULL OR [TotalFloors] IS NULL OR [Floor] <= [TotalFloors]) AND ([Latitude] IS NULL OR ([Latitude] >= -90 AND [Latitude] <= 90)) AND ([Longitude] IS NULL OR ([Longitude] >= -180 AND [Longitude] <= 180))");
        }
    }
}
