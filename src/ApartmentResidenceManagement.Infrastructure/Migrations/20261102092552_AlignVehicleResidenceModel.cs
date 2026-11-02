using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentResidenceManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignVehicleResidenceModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AddResidentDetails removed these account/contact settings; restore the
            // develop contract without rewriting an already published migration.
            migrationBuilder.DropForeignKey("FK_UserAccounts_Residents_ResidentId", "UserAccounts");
            migrationBuilder.AddColumn<string>(
                name: "Email", table: "Residents", type: "varchar(100)", maxLength: 100, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<string>(
                name: "Username", table: "UserAccounts", type: "varchar(50)", maxLength: 50, nullable: false,
                oldClrType: typeof(string), oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<bool>(
                name: "IsActive", table: "UserAccounts", type: "tinyint(1)", nullable: false, defaultValue: true,
                oldClrType: typeof(bool), oldType: "tinyint(1)");
            migrationBuilder.CreateIndex("IX_UserAccounts_Username", "UserAccounts", "Username", unique: true);
            migrationBuilder.AddForeignKey(
                name: "FK_UserAccounts_Residents_ResidentId", table: "UserAccounts", column: "ResidentId",
                principalTable: "Residents", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_UserAccounts_Residents_ResidentId", "UserAccounts");
            migrationBuilder.DropIndex("IX_UserAccounts_Username", "UserAccounts");
            migrationBuilder.DropColumn("Email", "Residents");
            migrationBuilder.AlterColumn<string>(
                name: "Username", table: "UserAccounts", type: "longtext", nullable: false,
                oldClrType: typeof(string), oldType: "varchar(50)", oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<bool>(
                name: "IsActive", table: "UserAccounts", type: "tinyint(1)", nullable: false,
                oldClrType: typeof(bool), oldType: "tinyint(1)", oldDefaultValue: true);
            migrationBuilder.AddForeignKey(
                name: "FK_UserAccounts_Residents_ResidentId", table: "UserAccounts", column: "ResidentId",
                principalTable: "Residents", principalColumn: "Id");
        }
    }
}
