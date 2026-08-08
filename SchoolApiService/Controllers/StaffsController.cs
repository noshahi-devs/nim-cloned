using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolApp.DAL.SchoolContext;
using SchoolApp.Models.DataModels;
using SchoolApp.Models.DataModels.StaticModel;

namespace SchoolApiService.Controllers
{
    using SchoolApiService.Services;
    
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class StaffsController(SchoolDbContext context, ImageUploadService imageService, Microsoft.AspNetCore.Identity.UserManager<SchoolApp.Models.DataModels.SecurityModels.ApplicationUser> userManager) : ControllerBase
    {
        private readonly SchoolDbContext _context = context;
        private readonly ImageUploadService _imageService = imageService;
        private readonly Microsoft.AspNetCore.Identity.UserManager<SchoolApp.Models.DataModels.SecurityModels.ApplicationUser> _userManager = userManager;

        public class StaffDto
        {
            public int StaffId { get; set; }
            public string? StaffName { get; set; }
            public string? Email { get; set; }
            public string? ImagePath { get; set; }
            public int UniqueStaffAttendanceNumber { get; set; }
            public string? Gender { get; set; }
            public DateTime? DOB { get; set; }
            public string? ContactNumber1 { get; set; }
            public string? Designation { get; set; }
            public string? Status { get; set; }
            public int? DepartmentId { get; set; }
            public DepartmentDto? Department { get; set; }
            public decimal? BasicSalary { get; set; }
            public int? ShiftId { get; set; }
            public ShiftDto? Shift { get; set; }
        }

        public class DepartmentDto
        {
            public int DepartmentId { get; set; }
            public string? DepartmentName { get; set; }
        }

        public class ShiftDto
        {
            public int ShiftId { get; set; }
            public string? ShiftName { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan? EndTime { get; set; }
        }

        public class StaffStatsDto
        {
            public int TotalStaff { get; set; }
            public int TeacherCount { get; set; }
            public int DepartmentCount { get; set; }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<StaffStatsDto>> GetStaffStats()
        {
            try
            {
                var totalStaff = await _context.dbsStaff.CountAsync();
                
                // Count Teachers: assuming Designation enum index 0 is Teacher or string comparisons
                // From StaffListComponent.ts it seems designation === 0 or 'teacher'
                var teacherCount = await _context.dbsStaff.CountAsync(s => s.Designation == Designation.Teacher);
                
                var departmentCount = await _context.dbsDepartment.CountAsync();

                return Ok(new StaffStatsDto
                {
                    TotalStaff = totalStaff,
                    TeacherCount = teacherCount,
                    DepartmentCount = departmentCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<StaffDto>>> GetdbsStaff([FromQuery] int skip = 0, [FromQuery] int take = 200)
        {
            try
            {
                var staffs = await _context.dbsStaff
                    .AsNoTracking()
                    .OrderByDescending(s => s.StaffId)
                    .Skip(skip)
                    .Take(take)
                    .Select(s => new StaffDto
                    {
                        StaffId = s.StaffId,
                        StaffName = s.StaffName,
                        Email = s.Email,
                        ImagePath = (s.ImagePath != null && s.ImagePath.Length < 2000) ? s.ImagePath : null, // Prevent hang from large legacy Base64 blobs
                        UniqueStaffAttendanceNumber = s.UniqueStaffAttendanceNumber,
                        Gender = s.Gender != null ? s.Gender.ToString() : null,
                        DOB = s.DOB,
                        ContactNumber1 = s.ContactNumber1,
                        Designation = s.Designation != null ? s.Designation.ToString() : null,
                        Status = s.Status,
                        DepartmentId = s.DepartmentId,
                        Department = s.Department != null ? new DepartmentDto
                        {
                            DepartmentId = s.Department.DepartmentId,
                            DepartmentName = s.Department.DepartmentName
                        } : null,
                        BasicSalary = s.BasicSalary,
                        ShiftId = s.ShiftId,
                        Shift = s.Shift != null ? new ShiftDto
                        {
                            ShiftId = s.Shift.ShiftId,
                            ShiftName = s.Shift.ShiftName,
                            StartTime = s.Shift.StartTime,
                            EndTime = s.Shift.EndTime
                        } : null
                    })
                    .ToListAsync();

                return Ok(staffs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal error fetching staff list", message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetStaff(int id)
        {
            try
            {
                var staff = await _context.dbsStaff
                    .Where(m => m.StaffId == id)
                    .Select(s => new
                    {
                        s.StaffId,
                        s.StaffName,
                        s.Email,
                        s.ImagePath,
                        s.UniqueStaffAttendanceNumber,
                        Gender = s.Gender != null ? s.Gender.ToString() : null,
                        s.DOB,
                        s.FatherName,
                        s.MotherName,
                        s.CNIC,
                        s.Experience,
                        s.TemporaryAddress,
                        s.PermanentAddress,
                        s.ContactNumber1,
                        s.Qualifications,
                        s.JoiningDate,
                        Designation = s.Designation != null ? s.Designation.ToString() : null,
                        s.BankAccountName,
                        s.BankAccountNumber,
                        s.BankName,
                        s.BankBranch,
                        s.Status,
                        s.BasicSalary,
                        s.DepartmentId,
                        Department = s.Department != null ? new { s.Department.DepartmentId, s.Department.DepartmentName } : null,
                        s.ShiftId,
                        Shift = s.Shift != null ? new { s.Shift.ShiftId, s.Shift.ShiftName, s.Shift.StartTime, s.Shift.EndTime, s.Shift.GraceMinutes } : null,
                        s.StaffSalaryId,
                        StaffSalary = s.StaffSalary != null ? new 
                        { 
                            s.StaffSalary.StaffSalaryId, 
                            s.StaffSalary.BasicSalary, 
                            s.StaffSalary.NetSalary,
                            s.StaffSalary.SavingFund,
                            s.StaffSalary.Taxes
                        } : null,
                        StaffExperiences = s.StaffExperiences != null ? s.StaffExperiences.Select(e => new
                        {
                            e.StaffExperienceId,
                            e.CompanyName,
                            e.Designation,
                            e.JoiningDate,
                            e.LeavingDate,
                            e.Responsibilities,
                            e.Achievements
                        }).ToList() : null
                    })
                    .FirstOrDefaultAsync();

                if (staff == null)
                {
                    return NotFound(new { error = "Sorry! No Staff is found. Try next time. Good luck." });
                }

                return Ok(staff);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal error fetching staff details", message = ex.Message, inner = ex.InnerException?.Message });
            }
        }
     

        [HttpGet("ByEmail/{email}")]
        public async Task<ActionResult> GetStaffByEmail(string email)
        {
            try
            {
                var staff = await _context.dbsStaff
                    .Where(m => m.Email == email)
                    .Select(s => new
                    {
                        s.StaffId,
                        s.StaffName,
                        s.Email,
                        s.ImagePath,
                        s.UniqueStaffAttendanceNumber,
                        Gender = s.Gender != null ? s.Gender.ToString() : null,
                        s.DOB,
                        s.FatherName,
                        s.MotherName,
                        s.CNIC,
                        s.Experience,
                        s.TemporaryAddress,
                        s.PermanentAddress,
                        s.ContactNumber1,
                        s.Qualifications,
                        s.JoiningDate,
                        Designation = s.Designation != null ? s.Designation.ToString() : null,
                        s.BankAccountName,
                        s.BankAccountNumber,
                        s.BankName,
                        s.BankBranch,
                        s.Status,
                        s.BasicSalary,
                        s.DepartmentId,
                        Department = s.Department != null ? new { s.Department.DepartmentId, s.Department.DepartmentName } : null,
                        s.ShiftId,
                        Shift = s.Shift != null ? new { s.Shift.ShiftId, s.Shift.ShiftName, s.Shift.StartTime, s.Shift.EndTime, s.Shift.GraceMinutes } : null,
                        s.StaffSalaryId,
                        StaffSalary = s.StaffSalary != null ? new 
                        { 
                            s.StaffSalary.StaffSalaryId, 
                            s.StaffSalary.BasicSalary, 
                            s.StaffSalary.NetSalary,
                            s.StaffSalary.SavingFund,
                            s.StaffSalary.Taxes
                        } : null,
                        StaffExperiences = s.StaffExperiences != null ? s.StaffExperiences.Select(e => new
                        {
                            e.StaffExperienceId,
                            e.CompanyName,
                            e.Designation,
                            e.JoiningDate,
                            e.LeavingDate,
                            e.Responsibilities,
                            e.Achievements
                        }).ToList() : null
                    })
                    .FirstOrDefaultAsync();

                if (staff == null)
                {
                    return NotFound(new { error = "Sorry! No Staff found with this email." });
                }

                return Ok(staff);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal error fetching staff by email", message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        #region Default_Post
        //[HttpPost]
        //public async Task<ActionResult<Staff>> PostStaff(Staff staff)
        //{
        //    _context.dbsStaff.Add(staff);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction("GetStaff", new { id = staff.StaffId }, staff);
        //} 
        #endregion

        [HttpPost]
        public async Task<ActionResult<Staff>> PostStaff(Staff staff)
        {
            // Check if the DepartmentId is valid
            if (staff.DepartmentId != null)
            {
                staff.Department = await _context.dbsDepartment.FindAsync(staff.DepartmentId);
                if (staff.Department == null)
                {
                    return BadRequest("Invalid DepartmentId");
                }
            }

            // Check if the StaffSalaryId is valid
            if (staff.StaffSalaryId != null)
            {
                staff.StaffSalary = await _context.dbsStaffSalary.FindAsync(staff.StaffSalaryId);
                if (staff.StaffSalary == null)
                {
                    return BadRequest("Invalid StaffSalaryId");
                }
            }

            // Check if binary image data is provided for upload
            if (staff.ImageUpload?.ImageData != null)
            {
                var path = await _imageService.Upload(staff.ImageUpload);
                if (path != null)
                {
                    staff.ImagePath = path;
                }
            }
            // Add the StaffExperiences to the context if they are provided
            if (staff.StaffExperiences != null && staff.StaffExperiences.Any())
            {
                foreach (var experience in staff.StaffExperiences)
                {
                    _context.dbsStaffExperience.Add(experience);
                }
            }

            // Automatically generate UniqueStaffAttendanceNumber
            int nextAttendanceNumber = 200;
            if (await _context.dbsStaff.AnyAsync())
            {
                nextAttendanceNumber = await _context.dbsStaff.MaxAsync(s => s.UniqueStaffAttendanceNumber) + 1;
            }
            staff.UniqueStaffAttendanceNumber = nextAttendanceNumber;

            // Add the Staff to the context
            _context.dbsStaff.Add(staff);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest($"Unable to save changes: {ex.InnerException?.Message ?? ex.Message}");
            }

            return CreatedAtAction("GetStaff", new { id = staff.StaffId }, staff);
        }


        #region Default_Put
        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutStaff(int id, Staff staff)
        //{
        //    if (id != staff.StaffId)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(staff).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!StaffExists(id))
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


        // PUT: api/Staffs/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStaff(int id, Staff staff)
        {
            if (id != staff.StaffId)
            {
                return BadRequest("Invalid StaffId");
            }
            
            // Since StaffSalary is created in advance
            if (staff.StaffSalaryId != null)
            {
                staff.StaffSalary = await _context.dbsStaffSalary.FindAsync(staff.StaffSalaryId);
                if (staff.StaffSalary == null)
                {
                    return BadRequest("Invalid StaffSalaryId");
                }
            }

            
            // Since Department is created in advance
            if (staff.DepartmentId != null)
            {
                staff.Department = await _context.dbsDepartment.FindAsync(staff.DepartmentId);
                if (staff.Department == null)
                {
                    return BadRequest("Invalid DepartmentId");
                }
            }


            // Check if binary image data is provided for upload
            if (staff.ImageUpload?.ImageData != null)
            {
                // Delete old file if it exists and we're replacing it
                if (!string.IsNullOrEmpty(staff.ImagePath))
                {
                    _imageService.DeleteOldImage(staff.ImagePath);
                }

                var path = await _imageService.Upload(staff.ImageUpload);
                if (path != null)
                {
                    staff.ImagePath = path;
                }
            }

            // Update the StaffExperiences if they are provided
            if (staff.StaffExperiences != null && staff.StaffExperiences.Any())
            {
                foreach (var experience in staff.StaffExperiences)
                {
                    if (experience.StaffExperienceId == 0)
                    {
                        _context.dbsStaffExperience.Add(experience);
                    }
                    else
                    {
                        _context.Entry(experience).State = EntityState.Modified;
                    }
                }
            }

            _context.Entry(staff).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StaffExists(id))
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


        #region Default_Delete
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteStaff(int id)
        //{
        //    var staff = await _context.dbsStaff.FindAsync(id);
        //    if (staff == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.dbsStaff.Remove(staff);
        //    await _context.SaveChangesAsync();

        //    return NoContent();
        //} 
        #endregion

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            var staff = await _context.dbsStaff
                .Include(s => s.StaffExperiences)
                .FirstOrDefaultAsync(s => s.StaffId == id);

            if (staff == null)
            {
                return NotFound();
            }

            // Check if the staff is a Class Teacher for any Section
            var isSectionTeacher = await _context.Sections.AnyAsync(s => s.StaffId == id);
            if (isSectionTeacher)
            {
                return BadRequest(new { message = "Cannot delete this staff member. They are a Class Teacher for one or more sections. Please reassign the section's class teacher first." });
            }

            // Check for linked Subject Assignments
            var hasSubjectAssignments = await _context.SubjectAssignments.AnyAsync(sa => sa.StaffId == id);
            if (hasSubjectAssignments)
            {
                return BadRequest(new { message = "Cannot delete this staff member. They have subject assignments. Please remove the subject assignments first." });
            }

            // Check for linked Leaves
            var hasLeaves = await _context.Leaves.AnyAsync(l => l.StaffId == id);
            if (hasLeaves)
            {
                return BadRequest(new { message = "Cannot delete this staff member. They have leave records. Please remove the leave records first." });
            }

            // Check for linked Marks
            var hasMarks = await _context.dbsMark.AnyAsync(m => m.StaffId == id);
            if (hasMarks)
            {
                return BadRequest(new { message = "Cannot delete this staff member. They have mark records. Please remove the mark records first." });
            }

            try
            {
                // Remove associated StaffExperience entries first
                _context.dbsStaffExperience.RemoveRange(staff.StaffExperiences ?? new List<StaffExperience>());
                _context.dbsStaff.Remove(staff);
                await _context.SaveChangesAsync();

                // Wait! Since the UI wants full sync: If staff is deleted, also delete their login account
                if (!string.IsNullOrEmpty(staff.Email)) 
                {
                    var user = await _userManager.FindByEmailAsync(staff.Email);
                    if (user != null) 
                    {
                        await _userManager.DeleteAsync(user);
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the staff member. They may have other linked records.", details = ex.InnerException?.Message ?? ex.Message });
            }

            return NoContent();
        }



        private bool StaffExists(int id)
        {
            return _context.dbsStaff.Any(e => e.StaffId == id);
        }
    }
}
