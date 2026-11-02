using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentResidenceManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceResidenceInvariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResidenceHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApartmentId = table.Column<int>(type: "int", nullable: false),
                    ResidentId = table.Column<int>(type: "int", nullable: false),
                    RelationshipType = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ActiveOwnerApartmentId = table.Column<int>(
                        type: "int",
                        nullable: true,
                        computedColumnSql: "CASE WHEN `IsActive` = 1 AND `RelationshipType` = 0 THEN `ApartmentId` ELSE NULL END"),
                    ActiveResidentId = table.Column<int>(
                        type: "int",
                        nullable: true,
                        computedColumnSql: "CASE WHEN `IsActive` = 1 THEN `ResidentId` ELSE NULL END")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResidenceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResidenceHistories_Apartments_ApartmentId",
                        column: x => x.ApartmentId,
                        principalTable: "Apartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResidenceHistories_Residents_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "Residents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.CheckConstraint(
                        name: "CK_ResidenceHistories_ActiveEndDate",
                        sql: "(`IsActive` = 1 AND `EndDate` IS NULL) OR (`IsActive` = 0 AND `EndDate` IS NOT NULL)");
                    table.CheckConstraint(
                        name: "CK_ResidenceHistories_DateRange",
                        sql: "`EndDate` IS NULL OR `EndDate` >= `StartDate`");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ResidenceHistories_ApartmentId",
                table: "ResidenceHistories",
                column: "ApartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResidenceHistories_ResidentId",
                table: "ResidenceHistories",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResidenceHistories_ActiveOwnerApartmentId",
                table: "ResidenceHistories",
                column: "ActiveOwnerApartmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResidenceHistories_ActiveResidentId",
                table: "ResidenceHistories",
                column: "ActiveResidentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResidenceHistories");
        }
    }
}
