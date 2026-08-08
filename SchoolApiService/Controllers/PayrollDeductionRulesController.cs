using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolApp.DAL.SchoolContext;
using SchoolApp.Models.DataModels;

namespace SchoolApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayrollDeductionRulesController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public PayrollDeductionRulesController(SchoolDbContext context)
        {
            _context = context;
        }

        // There is exactly one global rule row. GET creates a default (inactive) one on first
        // access so the settings page always has something to render/edit.
        [HttpGet]
        public async Task<IActionResult> GetRule()
        {
            var rule = await _context.PayrollDeductionRules.AsNoTracking().FirstOrDefaultAsync();
            if (rule == null)
            {
                rule = new PayrollDeductionRule();
                _context.PayrollDeductionRules.Add(rule);
                await _context.SaveChangesAsync();
            }
            return Ok(rule);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateRule([FromBody] PayrollDeductionRule updated)
        {
            var rule = await _context.PayrollDeductionRules.FirstOrDefaultAsync();
            if (rule == null)
            {
                rule = new PayrollDeductionRule();
                _context.PayrollDeductionRules.Add(rule);
            }

            rule.IsActive = updated.IsActive;
            rule.LeavesAllowed = updated.LeavesAllowed;
            rule.IsAbsentEqualToLeave = updated.IsAbsentEqualToLeave;
            rule.DeductionDayBasis = updated.DeductionDayBasis;
            rule.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(rule);
        }

        public class DeductionSuggestionDto
        {
            public bool RuleActive { get; set; }
            public int LeavesAllowed { get; set; }
            public bool IsAbsentEqualToLeave { get; set; }
            public DeductionDayBasis DeductionDayBasis { get; set; }
            public int BasisDays { get; set; }
            public decimal BasicSalary { get; set; }
            public decimal PerDayRate { get; set; }
            public int WorkingDays { get; set; }
            public int PresentDays { get; set; }
            public int ApprovedLeaveDays { get; set; }
            public int AbsentDays { get; set; }
            public int DeductionDays { get; set; }
            public decimal SuggestedDeductionAmount { get; set; }
        }

        // Computes a suggested deduction for one staff member over a date range (typically the pay
        // period being processed on the Pay Salary form). The caller decides whether/how to apply it —
        // this never writes anything, it only calculates. Sundays and recorded Holidays are treated as
        // non-working days and never counted as absences.
        //
        // Absence is derived from presence, not from explicit "marked absent" records: any working day
        // in the requested range with no Present attendance record and no approved leave covering it
        // counts as absent — no present means absent, whether that day is in the past or hasn't
        // happened yet. Recalculating later in the month (as more attendance comes in) will lower the
        // deduction as Present records replace what were "no record" days. The window only excludes
        // days before the staff member's joining date.
        [HttpGet("CalculateSuggestion")]
        public async Task<ActionResult<DeductionSuggestionDto>> CalculateSuggestion(
            [FromQuery] int staffId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var rule = await _context.PayrollDeductionRules.AsNoTracking().FirstOrDefaultAsync()
                       ?? new PayrollDeductionRule();

            var rangeStart = startDate.Date;
            var rangeEnd = endDate.Date;

            var staff = await _context.dbsStaff.AsNoTracking()
                .Where(s => s.StaffId == staffId)
                .Select(s => new { s.BasicSalary, s.JoiningDate })
                .FirstOrDefaultAsync();
            var basicSalary = staff?.BasicSalary ?? 0m;

            // Never evaluate days that predate employment.
            var effectiveStart = staff?.JoiningDate != null && staff.JoiningDate.Value.Date > rangeStart
                ? staff.JoiningDate.Value.Date
                : rangeStart;
            var effectiveEnd = rangeEnd;

            var holidayDates = await _context.Holidays
                .AsNoTracking()
                .Where(h => h.Date >= rangeStart && h.Date <= rangeEnd)
                .Select(h => h.Date.Date)
                .ToListAsync();

            // Working days across the WHOLE requested pay period (ignoring join date) — this is the
            // denominator for the "actual working days in month" rate basis, so a mid-month joiner
            // doesn't get a distorted per-day rate.
            int periodWorkingDays = 0;
            for (var d = rangeStart; d <= rangeEnd; d = d.AddDays(1))
            {
                if (d.DayOfWeek == DayOfWeek.Sunday || holidayDates.Contains(d)) continue;
                periodWorkingDays++;
            }

            var basisDays = rule.DeductionDayBasis == DeductionDayBasis.ActualDaysInMonth
                ? periodWorkingDays
                : 30;
            // Kept at full precision for the actual deduction math; rounded only where displayed
            // (below and in the DTO), so N days x displayed-rate can be a cent or two off the total —
            // normal, expected payroll rounding behavior, not a bug.
            var perDayRate = basisDays > 0 ? basicSalary / basisDays : 0m;

            if (effectiveEnd < effectiveStart)
            {
                // Whole period is before joining — nothing to evaluate.
                return Ok(new DeductionSuggestionDto
                {
                    RuleActive = rule.IsActive,
                    LeavesAllowed = rule.LeavesAllowed,
                    IsAbsentEqualToLeave = rule.IsAbsentEqualToLeave,
                    DeductionDayBasis = rule.DeductionDayBasis,
                    BasisDays = basisDays,
                    BasicSalary = basicSalary,
                    PerDayRate = Math.Round(perDayRate, 2)
                });
            }

            // Approved leave days: sum the overlap (in days) between each approved leave and the
            // elapsed portion of the range.
            var approvedLeaves = await _context.Leaves
                .AsNoTracking()
                .Where(l => l.StaffId == staffId
                            && l.Status == LeaveStatus.Approved
                            && l.StartDate <= effectiveEnd
                            && l.EndDate >= effectiveStart)
                .Select(l => new { l.StartDate, l.EndDate })
                .ToListAsync();

            int approvedLeaveDays = 0;
            foreach (var leave in approvedLeaves)
            {
                var overlapStart = leave.StartDate.Date < effectiveStart ? effectiveStart : leave.StartDate.Date;
                var overlapEnd = leave.EndDate.Date > effectiveEnd ? effectiveEnd : leave.EndDate.Date;
                if (overlapEnd >= overlapStart)
                {
                    approvedLeaveDays += (overlapEnd - overlapStart).Days + 1;
                }
            }

            // Working days actually evaluable for this staff member (respects join date).
            int workingDays = 0;
            for (var d = effectiveStart; d <= effectiveEnd; d = d.AddDays(1))
            {
                if (d.DayOfWeek == DayOfWeek.Sunday || holidayDates.Contains(d)) continue;
                workingDays++;
            }

            var presentDates = await _context.dbsAttendance
                .AsNoTracking()
                .Where(a => a.Type == AttendanceType.Staff
                            && a.AttendanceIdentificationNumber == staffId
                            && a.IsPresent
                            && a.Date >= effectiveStart && a.Date <= effectiveEnd)
                .Select(a => a.Date.Date)
                .Distinct()
                .ToListAsync();

            int presentDays = presentDates.Count(d => d.DayOfWeek != DayOfWeek.Sunday && !holidayDates.Contains(d));

            // Every working day that isn't covered by a Present record or an approved leave counts as
            // absent — whether or not anyone explicitly marked it, per the "unmarked days are absent" rule.
            int absentDays = Math.Max(0, workingDays - presentDays - approvedLeaveDays);

            int deductionDays;
            if (rule.IsAbsentEqualToLeave)
            {
                var combined = approvedLeaveDays + absentDays;
                deductionDays = Math.Max(0, combined - rule.LeavesAllowed);
            }
            else
            {
                var excessLeave = Math.Max(0, approvedLeaveDays - rule.LeavesAllowed);
                deductionDays = absentDays + excessLeave;
            }

            // Rounded once, from the full-precision rate x day count — not from a pre-rounded rate —
            // so the total always adds up correctly even though the displayed per-day rate is rounded.
            var suggestedAmount = rule.IsActive ? Math.Round(deductionDays * perDayRate, 2) : 0m;

            return Ok(new DeductionSuggestionDto
            {
                RuleActive = rule.IsActive,
                LeavesAllowed = rule.LeavesAllowed,
                IsAbsentEqualToLeave = rule.IsAbsentEqualToLeave,
                DeductionDayBasis = rule.DeductionDayBasis,
                BasisDays = basisDays,
                BasicSalary = basicSalary,
                PerDayRate = Math.Round(perDayRate, 2),
                WorkingDays = workingDays,
                PresentDays = presentDays,
                ApprovedLeaveDays = approvedLeaveDays,
                AbsentDays = absentDays,
                DeductionDays = deductionDays,
                SuggestedDeductionAmount = suggestedAmount
            });
        }
    }
}
