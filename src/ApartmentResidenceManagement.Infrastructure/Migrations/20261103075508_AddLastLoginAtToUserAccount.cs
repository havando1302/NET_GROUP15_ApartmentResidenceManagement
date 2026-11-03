using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentResidenceManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLastLoginAtToUserAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "UserAccounts",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "UserAccounts");
        }
    }
}
