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
    public class HolidaysController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public HolidaysController(SchoolDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetHolidays([FromQuery] int? year = null)
        {
            var query = _context.Holidays.AsNoTracking().AsQueryable();
            if (year.HasValue)
            {
                query = query.Where(h => h.Date.Year == year.Value);
            }

            var holidays = await query.OrderBy(h => h.Date).ToListAsync();
            return Ok(holidays);
        }

        [HttpPost]
        public async Task<IActionResult> CreateHoliday([FromBody] Holiday holiday)
        {
            if (string.IsNullOrWhiteSpace(holiday.Name))
            {
                return BadRequest("Name is required");
            }

            holiday.HolidayId = 0;
            holiday.Date = holiday.Date.Date;

            var exists = await _context.Holidays.AnyAsync(h => h.Date == holiday.Date);
            if (exists)
            {
                return BadRequest("A holiday is already recorded for this date.");
            }

            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHolidays), new { }, holiday);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHoliday(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null)
            {
                return NotFound();
            }

            _context.Holidays.Remove(holiday);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
