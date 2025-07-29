using Restaurant_Menu.Models;

public class KitchenNotification
{
    public int NotificationId { get; set; }
    public int OrderId { get; set; }
    public int TableNo { get; set; }
    public DateTime NotificationTime { get; set; } = DateTime.UtcNow;
    public bool IsAcknowledged { get; set; } = false;
    public string Message { get; set; } = "Order is ready to serve";

    public int RestaurantID { get; set; }
    public Restaurant? Restaurant { get; set; }

    public Order Order { get; set; }
}
