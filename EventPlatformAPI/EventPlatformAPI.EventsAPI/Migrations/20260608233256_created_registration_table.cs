using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventPlatformAPI.EventsAPI.Migrations
{
    /// <inheritdoc />
    public partial class created_registration_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "Events",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    ParticipantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParticipantEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Registrations_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationSagaStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentState = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationSagaStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SagaEventLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SagaEventLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_EventId",
                table: "Registrations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSagaStates_CorrelationId",
                table: "RegistrationSagaStates",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSagaStates_RegistrationId",
                table: "RegistrationSagaStates",
                column: "RegistrationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SagaEventLogs_CorrelationId",
                table: "SagaEventLogs",
                column: "CorrelationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Registrations");

            migrationBuilder.DropTable(
                name: "RegistrationSagaStates");

            migrationBuilder.DropTable(
                name: "SagaEventLogs");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "Events");
        }
    }
}
