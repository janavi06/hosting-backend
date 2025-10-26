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

        [Required]
        public int InventoryItemID { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityPerUnit { get; set; }

        // Multi-tenant
        public int RestaurantID { get; set; }
        public Restaurant? Restaurant { get; set; }
    }
}
