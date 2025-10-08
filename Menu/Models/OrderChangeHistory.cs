// Models/OrderChangeHistory.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_Menu.Models
{
    public class OrderChangeHistory
    {
        [Key]
        public int OrderChangeHistoryID { get; set; }

        [Required]
        public int OrderID { get; set; }

        [Required]
        [StringLength(50)]
        public string ChangeType { get; set; } // "ITEM_ADDED", "ITEM_REMOVED", "QTY_CHANGED", "ORDER_CANCELLED", etc.

        [StringLength(500)]
        public string Description { get; set; }

        public int? ChangedByUserID { get; set; }

        [Required]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public string OldValues { get; set; } // JSON string of old values
        public string NewValues { get; set; } // JSON string of new values

        // Navigation properties
        [ForeignKey("OrderID")]
        public Order Order { get; set; }

        [ForeignKey("ChangedByUserID")]
        public User ChangedByUser { get; set; }

        public int RestaurantID { get; set; }
        public Restaurant? Restaurant { get; set; }
    }
}