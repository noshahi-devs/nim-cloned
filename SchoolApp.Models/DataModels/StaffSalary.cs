using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolApp.Models.DataModels
{
    [Table("StaffSalary")]
    public class StaffSalary
    {
        // Per individual Staff's salary prototype

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? StaffSalaryId { get; set; }
        public string? StaffName { get; set; }
        public int? StaffId { get; set; }
        [ForeignKey("StaffId")]
        public Staff? Staff { get; set; }
        public DateTime? PaymentDate { get; set; } = DateTime.Now;
        public string? PaymentMonth { get; set; }
        public decimal? BasicSalary { get; set; }
        public decimal? FestivalBonus { get; set; }
        public decimal? Allowance { get; set; }
        public decimal? MedicalAllowance { get; set; }
        public decimal? HousingAllowance { get; set; }
        public decimal? TransportationAllowance { get; set; }
        public decimal? SavingFund { get; set; } = 0;
        public decimal? Taxes { get; set; } = 0;

        // Totals for the dynamic Additions/Deductions entered on this payment
        public decimal? TotalAdditions { get; set; } = 0;
        public decimal? TotalDeductions { get; set; } = 0;

        // Calculated at save time: BasicSalary + TotalAdditions - TotalDeductions
        public decimal? NetSalary { get; set; }

        public IList<SalaryPaymentDetail>? Details { get; set; }
    }
}



    

