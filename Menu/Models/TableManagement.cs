using Restaurant_Menu.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_System.Models
{
    public enum TableStatus
    {
        Available,
        Occupied,
        Reserved,
        Cleaning,
        Maintenance
    }

    public enum TableSection
    {
        Main,
        Bar,
        Outdoor,
        Private,
        VIP
    }

    public class TableManagement
    {
        [Key]
        public int TableManagementID { get; set; }

        [Required]
        public int RestaurantID { get; set; }

        [Required]
        public int RestaurantTableID { get; set; }

        [Required]
        public TableStatus Status { get; set; }

        public TableSection Section { get; set; }

        public int? CurrentOrderID { get; set; }

        public int? ReservedByCustomerID { get; set; }

        public DateTime? ReservationTime { get; set; }

        public DateTime? OccupiedSince { get; set; }

        public int SeatingCapacity { get; set; }

        public string SpecialFeatures { get; set; } // "Window View", "Wheelchair Access", etc.

        public int XPosition { get; set; } // For floor plan
        public int YPosition { get; set; } // For floor plan

        public DateTime LastUpdated { get; set; }

        // Navigation properties
        [ForeignKey("RestaurantTableID")]
        public RestaurantTable RestaurantTable { get; set; }

        [ForeignKey("RestaurantID")]
        public Restaurant Restaurant { get; set; }

        [ForeignKey("CurrentOrderID")]
        public Order CurrentOrder { get; set; }
    }
}