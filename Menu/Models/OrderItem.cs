using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Restaurant_Menu.Models
{
    public class OrderItem
    {
        public int OrderItemID { get; set; }
        public int OrderID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }


        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsPrepared { get; set; } = false; // ✅ NEW COLUMN
        public DateTime AddedToKitchenAt { get; set; } = DateTime.UtcNow; // ✅ ADD THIS LINE

        public DateTime? PreparedAt { get; set; }  // ✅ NEW COLUMN (nullable)

        public int BatchID { get; set; } = 1;

        public int RestaurantID { get; set; }
        public Restaurant? Restaurant { get; set; }


        [JsonIgnore]
        public virtual Order? Order { get; set; }

        [JsonIgnore]
        public virtual Product? Product { get; set; }

        public virtual ICollection<OrderItemCustomization> Customizations { get; set; } = new List<OrderItemCustomization>();



        [NotMapped]
        public List<int>? CustomizationOptionIds { get; set; }
    }
}
