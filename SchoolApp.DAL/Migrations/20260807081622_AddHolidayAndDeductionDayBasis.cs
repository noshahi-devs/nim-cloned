using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolApp.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddHolidayAndDeductionDayBasis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PerDayDeductionAmount",
                table: "PayrollDeductionRule");

            migrationBuilder.AddColumn<int>(
                name: "DeductionDayBasis",
                table: "PayrollDeductionRule",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Holiday",
                columns: table => new
                {
                    HolidayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holiday", x => x.HolidayId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Holiday");

            migrationBuilder.DropColumn(
                name: "DeductionDayBasis",
                table: "PayrollDeductionRule");

            migrationBuilder.AddColumn<decimal>(
                name: "PerDayDeductionAmount",
                table: "PayrollDeductionRule",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
