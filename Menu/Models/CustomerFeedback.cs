using Restaurant_Menu.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_System.Models
{
    public class CustomerFeedback
    {
        [Key]
        public int FeedbackID { get; set; }

        [Required]
        public int CustomerID { get; set; }

        [Required]
        public int RestaurantID { get; set; }

        public int? OrderID { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string Comments { get; set; }

        public string Category { get; set; } // Food, Service, Ambiance, etc.

        public bool IsResolved { get; set; } = false;

        public string ResolutionNotes { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CustomerID")]
        public Customer Customer { get; set; }

        [ForeignKey("RestaurantID")]
        public Restaurant Restaurant { get; set; }

        [ForeignKey("OrderID")]
        public Order Order { get; set; }
    }
}