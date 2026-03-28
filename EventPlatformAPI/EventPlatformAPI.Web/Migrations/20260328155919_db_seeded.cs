using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EventPlatformAPI.Web.Migrations
{
    /// <inheritdoc />
    public partial class db_seeded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Lecturers",
                columns: new[] { "Id", "Field", "FirstName", "LastName", "Title" },
                values: new object[,]
                {
                    { 1, "Softversko inženjerstvo", "Milan", "Petrović", "Prof. dr" },
                    { 2, "Baze podataka", "Jelena", "Jovanović", "Doc. dr" }
                });

            migrationBuilder.InsertData(
                table: "Locations",
                columns: new[] { "Id", "Address", "Capacity", "Name" },
                values: new object[,]
                {
                    { 1, "Bulevar kralja Aleksandra 73", 200, "Amfiteatar A" },
                    { 2, "Kraljice Marije 16", 80, "Sala 101" }
                });

            migrationBuilder.InsertData(
                table: "Types",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Stručna konferencija", "Konferencija" },
                    { 2, "Edukativni seminar", "Seminar" },
                    { 3, "Praktična radionica", "Radionica" }
                });

            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "Id", "Agenda", "DateTime", "DurationInHours", "LocationId", "Name", "Price", "TypeId" },
                values: new object[,]
                {
                    { 1, "Pregled novina u .NET platformi", new DateTime(2026, 6, 10, 9, 0, 0, 0, DateTimeKind.Unspecified), 6.00m, 1, "Savremene .NET tehnologije", 3500.00m, 1 },
                    { 2, "Osnovni principi mikroservisne arhitekture", new DateTime(2026, 6, 20, 10, 0, 0, 0, DateTimeKind.Unspecified), 4.00m, 2, "Uvod u mikroservise", 2500.00m, 2 }
                });

            migrationBuilder.InsertData(
                table: "EventLecturers",
                columns: new[] { "Id", "DateTime", "EventId", "LecturerId", "Theme" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 6, 10, 9, 0, 0, 0, DateTimeKind.Unspecified), 1, 1, "Arhitektura modernih .NET aplikacija" },
                    { 2, new DateTime(2026, 6, 10, 10, 0, 0, 0, DateTimeKind.Unspecified), 1, 1, "Performanse i optimizacija" },
                    { 3, new DateTime(2026, 6, 20, 10, 0, 0, 0, DateTimeKind.Unspecified), 2, 2, "Modelovanje servisa i baza" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EventLecturers",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "EventLecturers",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "EventLecturers",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Types",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Lecturers",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Lecturers",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Types",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Types",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
