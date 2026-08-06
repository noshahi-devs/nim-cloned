using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolApp.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffBasicSalaryAndSalaryPaymentDetail : Migration
    {
        /// <inheritdoc />
        // NOTE: This migration was intentionally hand-trimmed. `dotnet ef migrations add` originally
        // bundled in a large set of unrelated operations (StudentFees table, PaymentDetail.FeeId,
        // Student.DefaultDiscount, nullable alters on MonthlyPayment/OthersPayment) because the local
        // model was out of sync with schema already present in the shared database (applied by
        // migrations that never made it into this repo/any branch — see git history for details).
        // Those objects already exist in the DB, so only the genuinely new operations below were kept
        // and actually executed. Do not re-add the stripped operations without re-verifying DB state.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "NetSalary",
                table: "StaffSalary",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true,
                oldComputedColumnSql: "([BasicSalary] + [FestivalBonus] + [Allowance] + [MedicalAllowance] + [HousingAllowance] + [TransportationAllowance] - [SavingFund] - [Taxes])");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAdditions",
                table: "StaffSalary",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalDeductions",
                table: "StaffSalary",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BasicSalary",
                table: "Staff",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SalaryPaymentDetail",
                columns: table => new
                {
                    SalaryPaymentDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffSalaryId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryPaymentDetail", x => x.SalaryPaymentDetailId);
                    table.ForeignKey(
                        name: "FK_SalaryPaymentDetail_StaffSalary_StaffSalaryId",
                        column: x => x.StaffSalaryId,
                        principalTable: "StaffSalary",
                        principalColumn: "StaffSalaryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPaymentDetail_StaffSalaryId",
                table: "SalaryPaymentDetail",
                column: "StaffSalaryId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffSalary_StaffId",
                table: "StaffSalary",
                column: "StaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffSalary_Staff_StaffId",
                table: "StaffSalary",
                column: "StaffId",
                principalTable: "Staff",
                principalColumn: "StaffId");
        }

        /// <inheritdoc />
        // NOTE: Rolling back re-computes NetSalary via the restored SQL computed column, which will
        // silently discard any TotalAdditions/TotalDeductions-driven values entered through the new
        // Pay Salary flow. Confirm that's acceptable before ever running this Down().
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StaffSalary_Staff_StaffId",
                table: "StaffSalary");

            migrationBuilder.DropIndex(
                name: "IX_StaffSalary_StaffId",
                table: "StaffSalary");

            migrationBuilder.DropTable(
                name: "SalaryPaymentDetail");

            migrationBuilder.DropColumn(
                name: "TotalAdditions",
                table: "StaffSalary");

            migrationBuilder.DropColumn(
                name: "TotalDeductions",
                table: "StaffSalary");

            migrationBuilder.DropColumn(
                name: "BasicSalary",
                table: "Staff");

            migrationBuilder.AlterColumn<decimal>(
                name: "NetSalary",
                table: "StaffSalary",
                type: "decimal(18,2)",
                nullable: true,
                computedColumnSql: "([BasicSalary] + [FestivalBonus] + [Allowance] + [MedicalAllowance] + [HousingAllowance] + [TransportationAllowance] - [SavingFund] - [Taxes])",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }
    }
}
