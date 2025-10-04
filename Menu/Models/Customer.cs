using Restaurant_Menu.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_System.Models
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }

        [Required]
        public int RestaurantID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Phone]
        public string? Phone { get; set; } // Also make nullable if needed

        [EmailAddress]
        public string? Email { get; set; } // Also make nullable if needed

        public DateTime? DateOfBirth { get; set; }

        public int TotalVisits { get; set; }

        public decimal TotalSpent { get; set; }

        [NotMapped]
        public decimal AverageOrderValue => TotalVisits > 0 ? TotalSpent / TotalVisits : 0;

        public DateTime? FirstVisit { get; set; }

        public DateTime? LastVisit { get; set; }

        public string? Preferences { get; set; } // ✅ Make nullable

        public string? Allergies { get; set; } // ✅ Make nullable

        public bool IsVIP { get; set; } = false;

        public int LoyaltyPoints { get; set; }

        // Navigation properties
        [ForeignKey("RestaurantID")]
        public Restaurant Restaurant { get; set; }

        public ICollection<Order> Orders { get; set; }
        public ICollection<CustomerFeedback> Feedbacks { get; set; }
    }
}