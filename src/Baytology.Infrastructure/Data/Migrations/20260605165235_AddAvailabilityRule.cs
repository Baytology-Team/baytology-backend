using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baytology.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailabilityRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvailabilityRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecurrenceType = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: true),
                    SpecificDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    SlotDuration = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilityRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityRules_AgentUserId",
                table: "AvailabilityRules",
                column: "AgentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityRules_PropertyId",
                table: "AvailabilityRules",
                column: "PropertyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvailabilityRules");
        }
    }
}
