using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant_Menu.Models
{
    public class Offer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OfferID { get; set; }

        public int RestaurantID { get; set; }  // 🔗 Linked to specific restaurant

        public string? Code { get; set; }      // Optional promo code
        public string Description { get; set; } = string.Empty;

        public decimal? DiscountAmount { get; set; }   // Flat ₹
        public float? DiscountPercent { get; set; }    // % based

        public decimal MinBillAmount { get; set; } = 0;   // Condition like ₹2000

        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }

        public bool IsActive { get; set; } = true;
        public bool AutoApply { get; set; } = true;

        [ForeignKey("RestaurantID")]
        public Restaurant? Restaurant { get; set; }
    }
}
