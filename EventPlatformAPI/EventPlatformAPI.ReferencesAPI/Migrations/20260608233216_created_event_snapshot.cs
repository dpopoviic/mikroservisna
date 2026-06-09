using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventPlatformAPI.ReferencesAPI.Migrations
{
    /// <inheritdoc />
    public partial class created_event_snapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalEventId = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventSnapshots_ExternalEventId",
                table: "EventSnapshots",
                column: "ExternalEventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventSnapshots");
        }
    }
}
