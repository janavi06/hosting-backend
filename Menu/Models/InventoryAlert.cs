using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_Menu.Models
{
    [Table("inventoryalerts")]
    public class InventoryAlert
    {
        [Key]
        [Column("inventoryalertid")]
        public int InventoryAlertID { get; set; }

        [Column("inventoryitemid")]
        public int InventoryItemID { get; set; }

        [Column("alerttype")]
        public string AlertType { get; set; } = "low_stock";

        [Column("currentquantity")]
        public decimal CurrentQuantity { get; set; }

        [Column("reorderlevel")]
        public decimal ReorderLevel { get; set; }

        [Column("isresolved")]
        public bool IsResolved { get; set; } = false;

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("restaurantid")]
        public int RestaurantID { get; set; }

        public InventoryItem? InventoryItem { get; set; }
    }
}