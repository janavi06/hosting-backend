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
        public string UnitOfMeasure { get; set; } = "unit";

        [Column(TypeName = "decimal(18,4)")]
        public decimal CurrentQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ReorderLevel { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal AverageUnitCost { get; set; } = 0;

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        [Required]
        [MaxLength(20)]
        public string BaseUnit { get; set; } = "g";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        public int RestaurantID { get; set; }
        public Restaurant? Restaurant { get; set; }

        [NotMapped]
        public bool IsLowStock => CurrentQuantity <= ReorderLevel;
    }

    public class StockTransaction
    {
        [Key]
        public int StockTransactionID { get; set; }

        [Required]
        public int InventoryItemID { get; set; }
        public InventoryItem? InventoryItem { get; set; }   // ? ADD THIS

        [Required]
        public StockTransactionType TransactionType { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal QuantityChange { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitCost { get; set; }

        public string? Reference { get; set; }
        public string? Notes { get; set; }
public string? Unit { get; set; }
        public string? AdjustmentReason { get; set; }

        public DateTime TransactionTime { get; set; } = DateTime.UtcNow;

        public int RestaurantID { get; set; }
        public Restaurant? Restaurant { get; set; }   // ? ADD THIS

        public string? CreatedBy { get; set; }
    }
}
