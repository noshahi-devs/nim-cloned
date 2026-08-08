using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolApiService.Services;
using SchoolApiService.ViewModels;
using SchoolApp.DAL.SchoolContext;
using SchoolApp.Models.DataModels;
using System.Globalization;

namespace SchoolApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   // [Authorize]
    public class AttendancesController : ControllerBase
    {
        private readonly SchoolDbContext _context;
        private readonly IBiometricDeviceService _biometricDeviceService;

        public AttendancesController(SchoolDbContext context, IBiometricDeviceService biometricDeviceService)
        {
            _context = context;
            _biometricDeviceService = biometricDeviceService;
        }

        // GET: api/Attendances
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Attendance>>> GetdbsAttendance()
        {
            return await _context.dbsAttendance.ToListAsync();
        }

        // GET: api/Attendances/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Attendance>> GetAttendance(int id)
        {
            var attendance = await _context.dbsAttendance.FindAsync(id);

            if (attendance == null)
            {
                return NotFound();
            }

            return attendance;
        }

        [HttpGet("GetList/{Type}")]
        public async Task<ActionResult> GetList(AttendanceType Type)
        {


            List <AttList> data = new List <AttList> ();
            switch (Type)
            {
                case AttendanceType.Student:
                    data = await _context.dbsStudent.Select(s => new AttList()
                    {
                        AttId = s.StudentId,
                        Name = $"{s.StudentId} - "+s.StudentName
                    }).ToListAsync(); 
                    break;
                case AttendanceType.Staff:
                    data = await _context.dbsStaff.Select(s => new AttList()
                    {
                        AttId = s.StaffId,
                        Name = $"{s.StaffId} - " + s.StaffName
                    }).ToListAsync();
                    break;
            }
            return Ok(data);
        }

        #region Default_Post
        //[HttpPost]
        //public async Task<ActionResult<Attendance>> PostAttendance(Attendance attendance)
        //{
        //    _context.dbsAttendance.Add(attendance);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction("GetAttendance", new { id = attendance.AttendanceId }, attendance);
        //} 
        #endregion


        [HttpPost]
        public async Task<IActionResult> PostAttendance(Attendance attendance)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if the provided attendance type is valid
            if (!Enum.IsDefined(typeof(AttendanceType), attendance.Type))
            {
                return BadRequest("Invalid attendance type.");
            }
           
            // Check if the provided attendance identification number exists
            bool exists = false;
            switch (attendance.Type)
            {
                case AttendanceType.Student:
                    exists = await _context.dbsStudent.AnyAsync(s => s.StudentId == attendance.AttendanceIdentificationNumber);
                    break;
                case AttendanceType.Staff:
                    exists = await _context.dbsStaff.AnyAsync(s => s.StaffId == attendance.AttendanceIdentificationNumber);
                    break;
                default:
                    return BadRequest("Invalid attendance type.");
            }

            if (!exists)
            {
                return BadRequest("Invalid attendance identification number.");
            }

            var today = attendance.Date.Date;
            var alreadyMarked = await _context.dbsAttendance
                .AnyAsync(a => a.Type == attendance.Type 
                            && a.AttendanceIdentificationNumber == attendance.AttendanceIdentificationNumber 
                            && a.Date.Date == today);

            if (alreadyMarked)
            {
                return BadRequest("Attendance already marked for today.");
            }

            _context.dbsAttendance.Add(attendance);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAttendance", new { id = attendance.AttendanceId }, attendance);
        }


        #region Default_PUT
        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutAttendance(int id, Attendance attendance)
        //{
        //    if (id != attendance.AttendanceId)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(attendance).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!AttendanceExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return NoContent();
        //} 
        #endregion

        [HttpPost("CheckOut")]
        public async Task<IActionResult> CheckOut([FromBody] Attendance checkOutInfo)
        {
            if (checkOutInfo.Type != AttendanceType.Staff && checkOutInfo.Type != AttendanceType.Student)
            {
                return BadRequest("Invalid attendance type.");
            }

            var todayDate = DateTime.Today;
            // Find the most recent check-in record for this person that hasn't been checked out of
            var attendanceRecord = await _context.dbsAttendance
                .Where(a => a.Type == checkOutInfo.Type 
                         && a.AttendanceIdentificationNumber == checkOutInfo.AttendanceIdentificationNumber 
                         && a.CheckOutTime == null
                         && a.Date.Date == todayDate)
                .OrderByDescending(a => a.Date)
                .FirstOrDefaultAsync();

            if (attendanceRecord == null)
            {
                return NotFound("Check-In record not found for today. Please Check-In first.");
            }

            // Record check out time
            attendanceRecord.CheckOutTime = DateTime.Now;
            _context.Entry(attendanceRecord).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return Ok(attendanceRecord);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAttendance(int id, Attendance attendance)
        {
            if (id != attendance.AttendanceId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validation for update is similar to create, but without checking existence of attendance itself

            if (attendance.Type == AttendanceType.Student)
            {
                var studentExists = await _context.dbsStudent.AnyAsync(s => s.StudentId == attendance.AttendanceIdentificationNumber);
                if (!studentExists)
                {
                    return BadRequest("Invalid student attendance number");
                }
            }
            else if (attendance.Type == AttendanceType.Staff)
            {
                var staffExists = await _context.dbsStaff.AnyAsync(s => s.StaffId == attendance.AttendanceIdentificationNumber);
                if (!staffExists)
                {
                    return BadRequest("Invalid staff attendance number");
                }
            }
            else
            {
                return BadRequest("Invalid attendance type");
            }

            _context.Entry(attendance).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AttendanceExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }


        // DELETE: api/Attendances/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            var attendance = await _context.dbsAttendance.FindAsync(id);
            if (attendance == null)
            {
                return NotFound();
            }

            _context.dbsAttendance.Remove(attendance);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("Report/Class/{classId}")]
        public async Task<ActionResult> GetClassWiseReport(int classId, [FromQuery] DateTime date)
        {
            var nextDate = date.Date.AddDays(1);
            var startDate = date.Date;

            var report = await _context.dbsStudent
                .AsNoTracking()
                .Where(s => s.StandardId == classId)
                .Select(s => new
                {
                    StudentId = s.StudentId,
                    StudentName = s.StudentName,
                    RollNo = s.EnrollmentNo,
                    Attendance = _context.dbsAttendance
                        .Where(a => a.Type == AttendanceType.Student &&
                                    a.AttendanceIdentificationNumber == s.StudentId &&
                                    a.Date >= startDate && a.Date < nextDate)
                        .Select(a => new { a.IsPresent })
                        .FirstOrDefault()
                })
                .Select(x => new
                {
                    x.StudentId,
                    x.StudentName,
                    x.RollNo,
                    IsPresent = x.Attendance != null ? x.Attendance.IsPresent : false,
                    Status = x.Attendance != null ? (x.Attendance.IsPresent ? "Present" : "Absent") : "Unmarked"
                })
                .ToListAsync();

            return Ok(report);
        }

        [HttpGet("Report/Student/{studentId}")]
        public async Task<ActionResult> GetStudentReport(int studentId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var report = await _context.dbsAttendance
                .Where(a => a.Type == AttendanceType.Student && 
                            a.AttendanceIdentificationNumber == studentId &&
                            a.Date >= startDate && a.Date <= endDate)
                .OrderBy(a => a.Date)
                .Select(a => new {
                    Date = a.Date,
                    IsPresent = a.IsPresent,
                    Status = a.IsPresent ? "Present" : "Absent",
                    Remarks = a.Description
                })
                .ToListAsync();

            return Ok(report);
        }

        [HttpGet("Report/Staff/{staffId}")]
        public async Task<ActionResult> GetStaffReport(int staffId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var report = await _context.dbsAttendance
                .Where(a => a.Type == AttendanceType.Staff && 
                            a.AttendanceIdentificationNumber == staffId &&
                            a.Date >= startDate && a.Date <= endDate)
                .OrderBy(a => a.Date)
                .Select(a => new {
                    Date = a.Date,
                    IsPresent = a.IsPresent,
                    Status = a.IsPresent ? "Present" : "Absent",
                    Remarks = a.Description
                })
                .ToListAsync();

            return Ok(report);
        }

        [HttpGet("Report/StaffDaily")]
        public async Task<ActionResult> GetDailyStaffReport([FromQuery] DateTime date)
        {
            var nextDate = date.Date.AddDays(1);
            var startDate = date.Date;

            var report = await _context.dbsStaff
                .AsNoTracking()
                .Select(s => new
                {
                    StaffId = s.StaffId,
                    StaffName = s.StaffName,
                    Designation = s.Designation,
                    JoiningDate = s.JoiningDate,
                    Attendance = _context.dbsAttendance
                        .Where(a => a.Type == AttendanceType.Staff &&
                                    a.AttendanceIdentificationNumber == s.StaffId &&
                                    a.Date >= startDate && a.Date < nextDate)
                        .Select(a => new { a.IsPresent, a.Description, a.CheckInTime, a.CheckOutTime })
                        .FirstOrDefault()
                })
                .Select(x => new
                {
                    x.StaffId,
                    x.StaffName,
                    Designation = x.Designation.ToString(),
                    x.JoiningDate,
                    IsBeforeJoining = x.JoiningDate != null && startDate < x.JoiningDate.Value.Date,
                    IsPresent = x.Attendance != null ? x.Attendance.IsPresent : false,
                    Status = x.Attendance != null ? (x.Attendance.IsPresent ? "Present" : "Absent") : "",
                    Remarks = x.Attendance != null ? (x.Attendance.Description ?? "") : "",
                    CheckInTime = x.Attendance != null ? x.Attendance.CheckInTime : null,
                    CheckOutTime = x.Attendance != null ? x.Attendance.CheckOutTime : null
                })
                .ToListAsync();

            return Ok(report);
        }

        // Upserts a batch of attendance records — used both for saving manual entry (Admin/Accountant
        // bulk marking) and for persisting the rows previewed from FetchFromMachine. Unlike PostAttendance
        // (which rejects a duplicate same-day mark to protect the self-service teacher flow), this updates
        // an existing same-day record instead of rejecting it, since re-fetching/re-saving a corrected
        // entry for the same day is the expected use case here.
        [HttpPost("BulkMark")]
        public async Task<IActionResult> BulkMark([FromBody] List<BulkAttendanceEntryVm> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return BadRequest("No entries provided.");
            }

            int created = 0, updated = 0;
            var skipped = new List<object>();

            // For staff entries, AttendanceIdentificationNumber is the StaffId (see GetDailyStaffReport
            // above). Look up joining dates once so a day before someone joined can never be recorded as
            // present or absent — that's not a real attendance fact, it's a data entry mistake.
            var staffIds = entries.Where(e => e.Type == AttendanceType.Staff)
                .Select(e => e.AttendanceIdentificationNumber).Distinct().ToList();
            var joiningDates = await _context.dbsStaff
                .Where(s => staffIds.Contains(s.StaffId))
                .Select(s => new { s.StaffId, s.StaffName, s.JoiningDate })
                .ToDictionaryAsync(s => s.StaffId);

            foreach (var entry in entries)
            {
                var dateOnly = entry.Date.Date;

                if (entry.Type == AttendanceType.Staff && dateOnly.DayOfWeek == DayOfWeek.Sunday)
                {
                    skipped.Add(new
                    {
                        staffId = entry.AttendanceIdentificationNumber,
                        date = dateOnly,
                        reason = "Sunday - non-working day"
                    });
                    continue;
                }

                if (entry.Type == AttendanceType.Staff &&
                    joiningDates.TryGetValue(entry.AttendanceIdentificationNumber, out var staffInfo) &&
                    staffInfo.JoiningDate != null && dateOnly < staffInfo.JoiningDate.Value.Date)
                {
                    skipped.Add(new
                    {
                        staffId = entry.AttendanceIdentificationNumber,
                        staffName = staffInfo.StaffName,
                        date = dateOnly,
                        joiningDate = staffInfo.JoiningDate.Value.Date,
                        reason = "Before joining date"
                    });
                    continue;
                }

                var existing = await _context.dbsAttendance.FirstOrDefaultAsync(a =>
                    a.Type == entry.Type &&
                    a.AttendanceIdentificationNumber == entry.AttendanceIdentificationNumber &&
                    a.Date.Date == dateOnly);

                if (existing != null)
                {
                    existing.IsPresent = entry.IsPresent;
                    existing.Description = entry.Description;
                    existing.CheckInTime = entry.CheckInTime;
                    existing.CheckOutTime = entry.CheckOutTime;
                    updated++;
                }
                else
                {
                    _context.dbsAttendance.Add(new Attendance
                    {
                        Type = entry.Type,
                        AttendanceIdentificationNumber = entry.AttendanceIdentificationNumber,
                        Date = dateOnly,
                        IsPresent = entry.IsPresent,
                        Description = entry.Description,
                        CheckInTime = entry.CheckInTime,
                        CheckOutTime = entry.CheckOutTime
                    });
                    created++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { created, updated, total = entries.Count, skipped });
        }

        [HttpPost("FetchFromMachine")]
        public async Task<ActionResult> FetchFromMachine([FromBody] ZktecoMachineFetchRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var targetDate = (request.Date ?? DateTime.Today).Date;
            var targetEndDate = targetDate.AddDays(1);

            IReadOnlyList<BiometricLogEntry> logs;
            try
            {
                logs = await _biometricDeviceService.FetchLogsAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }

            var dailyLogs = logs
                .Where(x => x.PunchTime >= targetDate && x.PunchTime < targetEndDate)
                .ToList();

            var grouped = dailyLogs
                .GroupBy(x => x.EnrollNumber)
                .Select(g => new
                {
                    EnrollNumber = g.Key,
                    InTime = g.Min(x => x.PunchTime),
                    OutTime = g.Max(x => x.PunchTime),
                    TotalPunches = g.Count()
                })
                .OrderBy(x => x.InTime)
                .ToList();

            var attendanceNumbers = grouped
                .Select(x => int.TryParse(x.EnrollNumber, out var parsed) ? parsed : (int?)null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();

            var staffMap = await _context.dbsStaff
                .AsNoTracking()
                .Where(s => attendanceNumbers.Contains(s.UniqueStaffAttendanceNumber))
                .Select(s => new
                {
                    s.StaffId,
                    s.StaffName,
                    s.UniqueStaffAttendanceNumber
                })
                .ToDictionaryAsync(
                    x => x.UniqueStaffAttendanceNumber,
                    x => new { x.StaffId, x.StaffName },
                    cancellationToken);

            var rows = grouped
                .Select((x, idx) =>
                {
                    var hasNumericEnroll = int.TryParse(x.EnrollNumber, out var enrollNo);
                    var staffFound = false;
                    int staffId = 0;
                    string? staffName = null;

                    if (hasNumericEnroll && staffMap.TryGetValue(enrollNo, out var staff))
                    {
                        staffFound = true;
                        staffId = staff.StaffId;
                        staffName = staff.StaffName;
                    }

                    var inTime = x.InTime.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture).ToLowerInvariant();
                    var outTime = x.OutTime.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture).ToLowerInvariant();

                    return new ZktecoAttendanceRowVm
                    {
                        SrNo = idx + 1,
                        StaffId = staffFound ? staffId : (int?)null,
                        EmployeeId = staffFound ? $"EMP-AFT-{staffId:0000}" : x.EnrollNumber,
                        Name = staffFound ? (staffName ?? string.Empty) : $"Unknown ({x.EnrollNumber})",
                        Attendance = string.Empty,
                        LeaveCategory = string.Empty,
                        Date = x.InTime.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                        InTime = inTime,
                        OutTime = outTime,
                        InTimeRaw = x.InTime,
                        OutTimeRaw = x.OutTime,
                        Remarks = staffFound ? string.Empty : "No staff mapping found for this enroll number."
                    };
                })
                .ToList();

            return Ok(new
            {
                machineId = request.MachineId,
                machineNo = request.MachineNo,
                machineName = request.MachineName,
                ip = request.IP,
                date = targetDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                totalLogsRead = logs.Count,
                totalLogsForDate = dailyLogs.Count,
                rows
            });
        }

        private bool AttendanceExists(int id)
        {
            return _context.dbsAttendance.Any(e => e.AttendanceId == id);
        }
    }
}
