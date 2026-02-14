using Restaurant_Menu.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class OfferProduct
{
    [Key]
    public int OfferProductID { get; set; }

    public int OfferID { get; set; }
    public int ProductID { get; set; }

    [ForeignKey(nameof(OfferID))]
    public Offer? Offer { get; set; }

    [ForeignKey(nameof(ProductID))]
    public Product? Product { get; set; }
}
