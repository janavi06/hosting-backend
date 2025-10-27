namespace Restaurant_Menu.Models
{
    public class OrderItemRequest
    {
        public int? OrderItemID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public List<CustomizationRequest> Customizations { get; set; }
    }
}
