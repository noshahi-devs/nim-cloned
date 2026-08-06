using System;
using System.Collections.Generic;
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
    public class SalaryPaymentsController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public SalaryPaymentsController(SchoolDbContext context)
        {
            _context = context;
        }

        public class SalaryEntryDto
        {
            public string Description { get; set; }
            public decimal Amount { get; set; }
        }

        public class CreateSalaryPaymentDto
        {
            public int StaffId { get; set; }
            public DateTime? PaymentDate { get; set; }
            public string PaymentMonth { get; set; }
            public decimal BasicSalary { get; set; }
            public List<SalaryEntryDto> Additions { get; set; } = new();
            public List<SalaryEntryDto> Deductions { get; set; } = new();
        }

        [HttpGet]
        public async Task<IActionResult> GetSalaryPayments([FromQuery] int skip = 0, [FromQuery] int take = 200)
        {
            try
            {
                var payments = await _context.dbsStaffSalary
                    .AsNoTracking()
                    .Where(p => p.StaffId != null)
                    .OrderByDescending(p => p.PaymentDate)
                    .Skip(skip)
                    .Take(take)
                    .Select(p => new
                    {
                        p.StaffSalaryId,
                        p.StaffId,
                        StaffName = p.Staff != null ? p.Staff.StaffName : p.StaffName,
                        p.PaymentDate,
                        p.PaymentMonth,
                        p.BasicSalary,
                        p.TotalAdditions,
                        p.TotalDeductions,
                        p.NetSalary
                    })
                    .ToListAsync();

                return Ok(payments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("staff/{staffId}")]
        public async Task<IActionResult> GetSalaryPaymentsForStaff(int staffId)
        {
            try
            {
                var payments = await _context.dbsStaffSalary
                    .AsNoTracking()
                    .Where(p => p.StaffId == staffId)
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => new
                    {
                        p.StaffSalaryId,
                        p.StaffId,
                        p.PaymentDate,
                        p.PaymentMonth,
                        p.BasicSalary,
                        p.TotalAdditions,
                        p.TotalDeductions,
                        p.NetSalary
                    })
                    .ToListAsync();

                return Ok(payments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSalaryPayment(int id)
        {
            var payment = await _context.dbsStaffSalary
                .AsNoTracking()
                .Include(p => p.Details)
                .Where(p => p.StaffSalaryId == id)
                .Select(p => new
                {
                    p.StaffSalaryId,
                    p.StaffId,
                    StaffName = p.Staff != null ? p.Staff.StaffName : p.StaffName,
                    p.PaymentDate,
                    p.PaymentMonth,
                    p.BasicSalary,
                    p.TotalAdditions,
                    p.TotalDeductions,
                    p.NetSalary,
                    Additions = p.Details.Where(d => d.Type == SalaryEntryType.Addition)
                        .Select(d => new { d.SalaryPaymentDetailId, d.Description, d.Amount }),
                    Deductions = p.Details.Where(d => d.Type == SalaryEntryType.Deduction)
                        .Select(d => new { d.SalaryPaymentDetailId, d.Description, d.Amount })
                })
                .FirstOrDefaultAsync();

            if (payment == null)
            {
                return NotFound($"Salary payment with ID {id} not found");
            }

            return Ok(payment);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSalaryPayment([FromBody] CreateSalaryPaymentDto dto)
        {
            if (dto.StaffId == 0)
            {
                return BadRequest("StaffId is required");
            }

            var staff = await _context.dbsStaff.FindAsync(dto.StaffId);
            if (staff == null)
            {
                return BadRequest("Invalid StaffId");
            }

            var additions = dto.Additions ?? new List<SalaryEntryDto>();
            var deductions = dto.Deductions ?? new List<SalaryEntryDto>();

            decimal totalAdditions = additions.Sum(a => a.Amount);
            decimal totalDeductions = deductions.Sum(d => d.Amount);
            decimal netSalary = dto.BasicSalary + totalAdditions - totalDeductions;

            var payment = new StaffSalary
            {
                StaffId = dto.StaffId,
                StaffName = staff.StaffName,
                PaymentDate = dto.PaymentDate ?? DateTime.Now,
                PaymentMonth = dto.PaymentMonth,
                BasicSalary = dto.BasicSalary,
                TotalAdditions = totalAdditions,
                TotalDeductions = totalDeductions,
                NetSalary = netSalary,
                Details = additions.Select(a => new SalaryPaymentDetail
                {
                    Type = SalaryEntryType.Addition,
                    Description = a.Description,
                    Amount = a.Amount
                }).Concat(deductions.Select(d => new SalaryPaymentDetail
                {
                    Type = SalaryEntryType.Deduction,
                    Description = d.Description,
                    Amount = d.Amount
                })).ToList()
            };

            _context.dbsStaffSalary.Add(payment);

            // Keep Staff.BasicSalary as the staff member's current base salary
            staff.BasicSalary = dto.BasicSalary;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest($"Unable to save changes: {ex.InnerException?.Message ?? ex.Message}");
            }

            return CreatedAtAction(nameof(GetSalaryPayment), new { id = payment.StaffSalaryId }, payment);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSalaryPayment(int id)
        {
            var payment = await _context.dbsStaffSalary
                .Include(p => p.Details)
                .FirstOrDefaultAsync(p => p.StaffSalaryId == id);

            if (payment == null)
            {
                return NotFound();
            }

            _context.dbsStaffSalary.Remove(payment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
