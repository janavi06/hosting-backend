using Restaurant_Menu.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Offer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int OfferID { get; set; }

    public int RestaurantID { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public string Description { get; set; } = string.Empty;

    // 🔥 CHANGE ENUM TO STRING
    [Required]
    public string Scope { get; set; } = "GLOBAL";

    [Required]
    public string DiscountType { get; set; } = "PERCENT";

    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountPercent { get; set; }

    public decimal MinBillAmount { get; set; } = 0;

    public int Priority { get; set; } = 0;

    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }

    public bool IsActive { get; set; } = true;
    public bool AutoApply { get; set; } = false;
    public ICollection<OfferProduct> OfferProducts { get; set; } = new List<OfferProduct>();

    [ForeignKey(nameof(RestaurantID))]
    public Restaurant? Restaurant { get; set; }
}
