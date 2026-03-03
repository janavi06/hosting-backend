using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_Menu.Models
{
    public class StockAudit
    {
        [Key]
        public int StockAuditID { get; set; }

        public int InventoryItemID { get; set; }
        public InventoryItem? InventoryItem { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal SystemQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal PhysicalQuantity { get; set; }

        [NotMapped]
        public decimal Variance => PhysicalQuantity - SystemQuantity;

        public string? Notes { get; set; }

        public DateTime AuditDate { get; set; } = DateTime.UtcNow;

        public int RestaurantID { get; set; }
    }
}
