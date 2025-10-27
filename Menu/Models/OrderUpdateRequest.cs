using Restaurant_Menu.Models;

public class OrderUpdateRequest
{
    public int OrderID { get; set; }
    public string CreatedAt { get; set; }
    public int TableNo { get; set; }
    public string OrderStatus { get; set; }
    public List<OrderItemRequest> Items { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal CGST { get; set; }
    public decimal SGST { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal TotalAmount { get; set; }
}