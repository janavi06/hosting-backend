using Restaurant_Menu.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_System.Models
{
    public class AnalyticsSnapshot
    {
        [Key]
        public int SnapshotID { get; set; }

        [Required]
        public int RestaurantID { get; set; }

        [Required]
        public DateTime SnapshotDate { get; set; }

        // Sales Metrics
        public decimal DailyRevenue { get; set; }
        public int DailyOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int CancelledOrders { get; set; }

        // Customer Metrics
        public int NewCustomers { get; set; }
        public int ReturningCustomers { get; set; }
        public decimal CustomerSatisfactionScore { get; set; }

        // Operational Metrics
        public decimal LaborCostPercentage { get; set; }
        public decimal FoodCostPercentage { get; set; }
        public decimal TableTurnoverRate { get; set; }

        // Inventory Metrics
        public int LowStockItems { get; set; }
        public decimal InventoryValue { get; set; }

        // Weather Impact (external data)
        public string WeatherCondition { get; set; }
        public decimal Temperature { get; set; }
        public decimal WeatherImpactScore { get; set; }

        // Navigation properties
        [ForeignKey("RestaurantID")]
        public Restaurant Restaurant { get; set; }
    }
}