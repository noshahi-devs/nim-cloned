using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolApp.Models.DataModels
{
    public enum SalaryEntryType
    {
        Addition,
        Deduction
    }

    [Table("SalaryPaymentDetail")]
    public class SalaryPaymentDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SalaryPaymentDetailId { get; set; }

        public int StaffSalaryId { get; set; }

        public SalaryEntryType Type { get; set; }

        [Required]
        public string Description { get; set; }

        public decimal Amount { get; set; }
    }
}
