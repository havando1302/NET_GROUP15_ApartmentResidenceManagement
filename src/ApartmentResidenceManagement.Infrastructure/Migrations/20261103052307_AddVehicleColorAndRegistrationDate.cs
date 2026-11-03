using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentResidenceManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleColorAndRegistrationDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Vehicles",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAtUtc",
                table: "Vehicles",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "RegisteredAtUtc",
                table: "Vehicles");
        }
    }
}
