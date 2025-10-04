using Restaurant_Menu.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_System.Models
{
    public class StaffPerformance
    {
        [Key]
        public int PerformanceID { get; set; }

        [Required]
        public int StaffID { get; set; }

        [Required]
        public int RestaurantID { get; set; }

        [Required]
        public DateTime PerformanceDate { get; set; }

        public int OrdersServed { get; set; }

        public decimal TotalSales { get; set; }

        public decimal AverageOrderValue { get; set; }

        public int PositiveReviews { get; set; }

        public int NegativeReviews { get; set; }

        public decimal EfficiencyScore { get; set; } // 0-100

        // Navigation properties
        [ForeignKey("StaffID")]
        public Staff Staff { get; set; }

        [ForeignKey("RestaurantID")]
        public Restaurant Restaurant { get; set; }
    }
}