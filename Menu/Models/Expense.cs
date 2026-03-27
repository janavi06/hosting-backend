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
        public string Description { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

        public PaymentMethod PaymentMethod { get; set; }

        public string? Vendor { get; set; }
        public string? ReceiptNumber { get; set; }
        public string? Notes { get; set; }

        public bool IsRecurring { get; set; } = false;
        public string? RecurringFrequency { get; set; }

        public string? ApprovedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // 🔥 INVENTORY LINK
        public int? InventoryItemID { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? Quantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? UnitCost { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? TotalCost { get; set; }

        // Navigation
        [ForeignKey("RestaurantID")]
        public Restaurant? Restaurant { get; set; }

        [ForeignKey("InventoryItemID")]
        public InventoryItem? InventoryItem { get; set; }
    }
}