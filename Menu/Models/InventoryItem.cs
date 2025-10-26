using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_Menu.Models
{
    public enum StockTransactionType
    {
        Purchase = 0,
        Adjustment = 1,
        Waste = 2,
        Sale = 3,
        Return = 4
    }

    public class InventoryItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InventoryItemID { get; set; }

        [Required]
        [MaxLength(150)]
        public string ItemName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? SKU { get; set; }

        [MaxLength(50)]
        public string UnitOfMeasure { get; set; } = "unit"; // e.g., kg, g, l, ml, pcs

        [Column(TypeName = "decimal(18,3)")]
        public decimal CurrentQuantity { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal ReorderLevel { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AverageUnitCost { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // Multi-tenant
        public int RestaurantID { get; set; }
        public Restaurant? Restaurant { get; set; }
    }

    public class StockTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StockTransactionID { get; set; }

        [Required]
        public int InventoryItemID { get; set; }
        public InventoryItem? InventoryItem { get; set; }

        [Required]
        public StockTransactionType TransactionType { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityChange { get; set; } // positive for in, negative for out

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; } // for averaging cost on purchases

        public string? Reference { get; set; } // e.g., PO-123, OrderID-12
        public string? Notes { get; set; }

        public DateTime TransactionTime { get; set; } = DateTime.UtcNow;

        // Multi-tenant
        public int RestaurantID { get; set; }
        public Restaurant? Restaurant { get; set; }

        public string? CreatedBy { get; set; }
    }
}
