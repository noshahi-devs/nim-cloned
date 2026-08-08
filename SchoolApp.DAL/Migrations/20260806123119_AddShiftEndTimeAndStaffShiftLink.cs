using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolApp.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftEndTimeAndStaffShiftLink : Migration
    {
        /// <inheritdoc />
        // NOTE: Hand-trimmed like the previous migration. `dotnet ef migrations add` wanted to CREATE
        // the whole "Shifts" table and re-add the StaffSalary->Staff FK/index — both already exist in
        // the shared DB (Shifts from the orphaned drift migration described earlier; the StaffSalary
        // FK/index because it was added by raw SQL in a prior session step and the model snapshot was
        // never re-synced for it). Only the genuinely new operations below were kept and executed.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeSpan>(
                name: "StartTime",
                table: "Shifts",
                type: "time",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndTime",
                table: "Shifts",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShiftId",
                table: "Staff",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staff_ShiftId",
                table: "Staff",
                column: "ShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_Staff_Shifts_ShiftId",
                table: "Staff",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "ShiftId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Staff_Shifts_ShiftId",
                table: "Staff");

            migrationBuilder.DropIndex(
                name: "IX_Staff_ShiftId",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Shifts");

            migrationBuilder.AlterColumn<string>(
                name: "StartTime",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");
        }
    }
}
