using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolApp.Models.DataModels
{
    // An official non-working day (e.g. a public holiday). Used together with Sundays to
    // exclude non-working days from the payroll absence calculation.
    [Table("Holiday")]
    public class Holiday
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int HolidayId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string Name { get; set; }
    }
}
