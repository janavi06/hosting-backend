using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_Menu.Models
{
    public class ProductRecipe
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductRecipeID { get; set; }

        [Required]
        public int ProductID { get; set; }

        public Product? Product { get; set; }   // ADD

        [Required]
        public int InventoryItemID { get; set; }

        public InventoryItem? InventoryItem { get; set; }  // ADD

        [Column(TypeName = "decimal(18,4)")]
        public decimal QuantityPerUnit { get; set; }

        // Multi-tenant
        public int RestaurantID { get; set; }
        public Restaurant? Restaurant { get; set; }
    }
}