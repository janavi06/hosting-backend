namespace Restaurant_Menu.Models
{
    public class OrderItemRequest
    {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public List<int>? CustomizationOptionIds { get; set; }
    }
}
