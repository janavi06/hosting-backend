using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_Menu.Models
{
    public enum OrderStatus
    {
        Pending,      // Order placed, but not yet confirmed
        Confirmed,    // Confirmed by the waiter
        Served,       // Order served to the customer
        Completed,    // Payment done, order closed
        Cancelled     // Order cancelled
    }
    public enum OrderSource           // 🌟 NEW
    {
        QR = 0,
        Waiter = 1
    }
    public enum KitchenStatus
    {
        Pending,      // Order received but not started
        Preparing,    // Chef is currently preparing the food
        Ready,        // Food is ready for pickup
        Delivered     // Food has been picked up by waiter
    }
    // 🔹 Added PaymentStatus Enum

    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderID { get; set; }

        public int? RestaurantTableID { get; set; }
        public int UserID { get; set; }  // Customer's UserID

        // New fields for tax and charges
        public decimal CGST { get; set; }           // e.g., 2.5 means 2.5%
        public decimal SGST { get; set; }           // e.g., 2.5 means 2.5%
        public decimal ServiceCharge { get; set; }    // e.g., 5 means 5%
        public decimal Subtotal { get; set; }
        public decimal TotalAmount { get; set; }
       public OrderSource? Source { get; set; } = OrderSource.QR;   // 🌟 NEW

        public DateTime? ClosedAt { get; set; } // Nullable for orders that aren't closed
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Audit fields with default valuesa
        public string? CreatedBy { get; set; } = "System";
        public string? UpdatedBy { get; set; } = "System";

        public OrderStatus? OrderStatus { get; set; }
        public KitchenStatus? KitchenStatus { get; set; }

        public DateTime? LastKitchenReadyAt { get; set; } // ✅ This line added

        /// <summary>
        /// True until the kitchen dashboard has buzzed/spoken this order.
        /// </summary>
        public bool PlaySound { get; set; } = true;

        public int? WaiterUserID { get; set; } // Nullable in case no waiter is assigned

        // Navigation property to reference the User table
        public User? Waiter { get; set; }

        // Flag to indicate if a waiter is assigned
        public bool IsAssigned { get; set; } = false;


        // ↓ Add near other properties like TotalAmount
        public int? AppliedOfferID { get; set; }
        public decimal DiscountAmount { get; set; } = 0;

        [ForeignKey("AppliedOfferID")]
        public Offer? AppliedOffer { get; set; }

        public int RestaurantID { get; set; }
        public Restaurant? Restaurant { get; set; }
        public int OrderNumber { get; set; }   // Local per-restaurant running number

        public int? CustomerID { get; set; }  // Nullable, optional field

        // Collection for Order Items (No Navigation Properties)
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // 🔹 Added Missing Payments Collection
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        public RestaurantTable? RestaurantTable { get; set; }  // <--- THIS



    }
}
