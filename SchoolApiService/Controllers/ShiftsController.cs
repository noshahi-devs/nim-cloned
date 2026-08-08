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
    public class ShiftsController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public ShiftsController(SchoolDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetShifts()
        {
            var shifts = await _context.Shifts
                .AsNoTracking()
                .OrderBy(s => s.ShiftName)
                .Select(s => new
                {
                    s.ShiftId,
                    s.ShiftName,
                    s.StartTime,
                    s.EndTime,
                    s.GraceMinutes,
                    s.IsActive,
                    StaffCount = s.Staffs != null ? s.Staffs.Count : 0
                })
                .ToListAsync();

            return Ok(shifts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetShift(int id)
        {
            var shift = await _context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.ShiftId == id);
            if (shift == null)
            {
                return NotFound($"Shift with ID {id} not found");
            }
            return Ok(shift);
        }

        [HttpPost]
        public async Task<IActionResult> CreateShift([FromBody] Shift shift)
        {
            if (string.IsNullOrWhiteSpace(shift.ShiftName))
            {
                return BadRequest("ShiftName is required");
            }

            shift.ShiftId = 0;
            shift.CreatedAt = DateTime.Now;
            shift.UpdatedAt = DateTime.Now;

            _context.Shifts.Add(shift);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest($"Unable to save changes: {ex.InnerException?.Message ?? ex.Message}");
            }

            return CreatedAtAction(nameof(GetShift), new { id = shift.ShiftId }, shift);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateShift(int id, [FromBody] Shift shift)
        {
            if (id != shift.ShiftId)
            {
                return BadRequest("ID mismatch");
            }

            var existing = await _context.Shifts.FindAsync(id);
            if (existing == null)
            {
                return NotFound($"Shift with ID {id} not found");
            }

            existing.ShiftName = shift.ShiftName;
            existing.StartTime = shift.StartTime;
            existing.EndTime = shift.EndTime;
            existing.GraceMinutes = shift.GraceMinutes;
            existing.IsActive = shift.IsActive;
            existing.UpdatedAt = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest($"Unable to save changes: {ex.InnerException?.Message ?? ex.Message}");
            }

            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShift(int id)
        {
            var shift = await _context.Shifts.FindAsync(id);
            if (shift == null)
            {
                return NotFound();
            }

            var assignedCount = await _context.dbsStaff.CountAsync(s => s.ShiftId == id);
            if (assignedCount > 0)
            {
                return BadRequest($"Cannot delete: {assignedCount} staff member(s) are assigned to this shift.");
            }

            _context.Shifts.Remove(shift);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
