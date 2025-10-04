using Restaurant_Menu.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_System.Models
{
    public class Staff
    {
        [Key]
        public int StaffID { get; set; }

        [Required]
        public int RestaurantID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Role { get; set; } // Waiter, Chef, Manager, etc.

        [StringLength(20)]
        public string Phone { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public decimal HourlyRate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime HireDate { get; set; }

        // Navigation properties
        [ForeignKey("RestaurantID")]
        public Restaurant Restaurant { get; set; }

        public ICollection<StaffShift> Shifts { get; set; }
        public ICollection<StaffPerformance> Performances { get; set; }
    }
}