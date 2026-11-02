using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentResidenceManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResidentDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAccounts_Residents_ResidentId",
                table: "UserAccounts");

            migrationBuilder.DropIndex(
                name: "IX_UserAccounts_Username",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Residents");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "UserAccounts",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "UserAccounts",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Residents",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "Residents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HomeTown",
                table: "Residents",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IdentityCard",
                table: "Residents",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Residents_IdentityCard",
                table: "Residents",
                column: "IdentityCard",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAccounts_Residents_ResidentId",
                table: "UserAccounts",
                column: "ResidentId",
                principalTable: "Residents",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAccounts_Residents_ResidentId",
                table: "UserAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Residents_IdentityCard",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "HomeTown",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "IdentityCard",
                table: "Residents");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "UserAccounts",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "UserAccounts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Residents",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_Username",
                table: "UserAccounts",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAccounts_Residents_ResidentId",
                table: "UserAccounts",
                column: "ResidentId",
                principalTable: "Residents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
