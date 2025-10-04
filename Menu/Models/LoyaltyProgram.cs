using Restaurant_Menu.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_System.Models
{
    public class LoyaltyProgram
    {
        [Key]
        public int LoyaltyID { get; set; }

        [Required]
        public int RestaurantID { get; set; }

        [Required]
        [StringLength(100)]
        public string ProgramName { get; set; }

        public decimal PointsPerDollar { get; set; } = 1;

        public decimal DiscountPerPoint { get; set; } = 0.01m;

        public int PointsForFreeItem { get; set; } = 100;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("RestaurantID")]
        public Restaurant Restaurant { get; set; }
    }
}