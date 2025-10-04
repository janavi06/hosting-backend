using Restaurant_Menu.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_System.Models
{
    public class StaffShift
    {
        [Key]
        public int ShiftID { get; set; }

        [Required]
        public int StaffID { get; set; }

        [Required]
        public int RestaurantID { get; set; }

        [Required]
        public DateTime ShiftDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [StringLength(50)]
        public string Role { get; set; }

        public decimal HoursWorked { get; set; }

        public bool IsCompleted { get; set; } = false;

        public string Notes { get; set; }

        // Navigation properties
        [ForeignKey("StaffID")]
        public Staff Staff { get; set; }

        [ForeignKey("RestaurantID")]
        public Restaurant Restaurant { get; set; }
    }
}