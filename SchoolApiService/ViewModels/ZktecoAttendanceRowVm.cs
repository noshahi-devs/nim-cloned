namespace SchoolApiService.ViewModels
{
    public class ZktecoAttendanceRowVm
    {
        public int SrNo { get; set; }
        public int? StaffId { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Attendance { get; set; } = string.Empty;
        public string LeaveCategory { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string InTime { get; set; } = string.Empty;
        public string OutTime { get; set; } = string.Empty;
        public DateTime InTimeRaw { get; set; }
        public DateTime OutTimeRaw { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }
}
