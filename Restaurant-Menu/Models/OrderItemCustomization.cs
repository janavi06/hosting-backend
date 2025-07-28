using System.Text.Json.Serialization;

namespace Restaurant_Menu.Models

{
    public class OrderItemCustomization
    {
        public int OrderItemCustomizationID { get; set; }
        public int OrderItemID { get; set; }
        public int CustomizationOptionID { get; set; }

        [JsonIgnore]
        public virtual OrderItem OrderItem { get; set; } = null!;

        public virtual CustomizationOption CustomizationOption { get; set; } = null!;
    }
}
