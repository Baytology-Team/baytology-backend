using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baytology.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAvailabilityRuleForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityRules_PropertyId_AgentUserId",
                table: "AvailabilityRules",
                columns: new[] { "PropertyId", "AgentUserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilityRules_AspNetUsers_AgentUserId",
                table: "AvailabilityRules",
                column: "AgentUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilityRules_Properties_PropertyId",
                table: "AvailabilityRules",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilityRules_AspNetUsers_AgentUserId",
                table: "AvailabilityRules");

            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilityRules_Properties_PropertyId",
                table: "AvailabilityRules");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilityRules_PropertyId_AgentUserId",
                table: "AvailabilityRules");
        }
    }
}
