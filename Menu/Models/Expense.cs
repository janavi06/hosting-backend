using Restaurant_Menu.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_System.Models
{
    public enum ExpenseCategory
    {
        Food,
        Beverage,
        Labor,
        Utilities,
        Rent,
        Supplies,
        Marketing,
        Maintenance,
        Insurance,
        Other
    }

    public enum PaymentMethod
    {
        Cash,
        Card,
        BankTransfer,
        Check
    }

    public class Expense
    {
        [Key]
        public int ExpenseID { get; set; }

        [Required]
        public int RestaurantID { get; set; }

        [Required]
        public ExpenseCategory Category { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime ExpenseDate { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        [StringLength(50)]
        public string Vendor { get; set; }

        [StringLength(100)]
        public string ReceiptNumber { get; set; }

        public string Notes { get; set; }

        public bool IsRecurring { get; set; } = false;

        public string RecurringFrequency { get; set; } // Monthly, Weekly, etc.

        public string ApprovedBy { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("RestaurantID")]
        public Restaurant Restaurant { get; set; }
    }
}