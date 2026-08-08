using System;
using SchoolApp.Models.DataModels;

namespace SchoolApiService.ViewModels
{
    public class BulkAttendanceEntryVm
    {
        public int AttendanceIdentificationNumber { get; set; }
        public AttendanceType Type { get; set; }
        public DateTime Date { get; set; }
        public bool IsPresent { get; set; }
        public string? Description { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
    }
}
