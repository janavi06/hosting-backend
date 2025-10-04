using Restaurant_Menu.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_System.Models
{
    public class Reservation
    {
        [Key]
        public int ReservationID { get; set; }

        [Required]
        public int RestaurantID { get; set; }

        [Required]
        public int RestaurantTableID { get; set; }

        [Required]
        public string CustomerName { get; set; }

        [Phone]
        public string CustomerPhone { get; set; }

        [EmailAddress]
        public string CustomerEmail { get; set; }

        [Required]
        public DateTime ReservationTime { get; set; }

        [Required]
        public int PartySize { get; set; }

        public string SpecialRequests { get; set; }

        public string Status { get; set; } = "Confirmed"; // Confirmed, Seated, Cancelled, NoShow

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("RestaurantTableID")]
        public RestaurantTable RestaurantTable { get; set; }

        [ForeignKey("RestaurantID")]
        public Restaurant Restaurant { get; set; }
    }
}