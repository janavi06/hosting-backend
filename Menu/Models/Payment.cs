using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_Menu.Models
{

    public enum PaymentChannel
    {
        Customer = 0,  // From menu component
        Waiter = 1     // From waiter component
    }

    public enum PaymentStatus
    {
        Pending,
        Success,
        Failed,
        Cancelled
    }

    public class Payment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PaymentID { get; set; }

        public int OrderID { get; set; }
        [ForeignKey("OrderID")]
        public Order Order { get; set; }

        public int TableNo { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "UPI";
        public int RestaurantID { get; set; }
        public Restaurant? Restaurant { get; set; }

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public PaymentChannel PaymentChannel { get; set; } = PaymentChannel.Customer;


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public bool IsNotified { get; set; } = false;  // ✅ Added Property for Notifications
    }
}
