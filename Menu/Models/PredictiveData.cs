using Restaurant_Menu.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_System.Models
{
    public class PredictiveData
    {
        [Key]
        public int PredictionID { get; set; }

        [Required]
        public int RestaurantID { get; set; }

        [Required]
        public DateTime PredictionDate { get; set; }

        public decimal PredictedRevenue { get; set; }
        public int PredictedOrders { get; set; }
        public int PredictedCustomers { get; set; }

        public string PeakHours { get; set; }
        public string RecommendedStaffing { get; set; }

        public decimal ConfidenceLevel { get; set; }

        public DateTime GeneratedAt { get; set; }

        // Navigation properties
        [ForeignKey("RestaurantID")]
        public Restaurant Restaurant { get; set; }
    }
}