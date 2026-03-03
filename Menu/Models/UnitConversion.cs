using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_Menu.Models
{
    public class UnitConversion
    {
        [Key]
        public int UnitConversionID { get; set; }

        [Required]
        public int InventoryItemID { get; set; }

        [Required]
        [MaxLength(20)]
        public string FromUnit { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string ToUnit { get; set; } = null!;

        [Column(TypeName = "decimal(18,6)")]
        public decimal ConversionFactor { get; set; }

        public int RestaurantID { get; set; }
    }
}