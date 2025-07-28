namespace Restaurant_Menu.Models;
    using System.Text.Json.Serialization;

public class CustomizationOption
{
    public int CustomizationOptionID { get; set; }
    public string Name { get; set; } = null!;     
    public decimal FixedPrice { get; set; }  
    public int ProductID { get; set; }

    [JsonIgnore]
    public virtual Product Product { get; set; } = null!;
}
