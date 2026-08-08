using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolApp.Models.DataModels
{
    public enum DeductionDayBasis
    {
        // Per-day rate = BasicSalary / 30, regardless of the actual month length.
        FixedThirty,
        // Per-day rate = BasicSalary / (number of days in the pay period's month).
        ActualDaysInMonth
    }

    // A single global rule (one row) governing automatic salary deduction based on
    // attendance/leave for a pay period. See PayrollDeductionRulesController for the
    // GET/PUT single-record pattern.
    [Table("PayrollDeductionRule")]
    public class PayrollDeductionRule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PayrollDeductionRuleId { get; set; }

        public bool IsActive { get; set; } = false;

        // Free leave-like days allowed per pay period before deduction kicks in.
        public int LeavesAllowed { get; set; } = 0;

        // When true, unapproved absences draw from the same LeavesAllowed pool as approved
        // leaves (so absences beyond the pool are what deduct). When false, absences always
        // deduct in full regardless of the allowance; only approved leaves beyond the
        // allowance ever draw from the pool.
        public bool IsAbsentEqualToLeave { get; set; } = false;

        // The per-day deduction amount is derived from each staff member's own BasicSalary at
        // calculation time (BasicSalary / basis days) rather than being a fixed rupee figure here.
        public DeductionDayBasis DeductionDayBasis { get; set; } = DeductionDayBasis.FixedThirty;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
