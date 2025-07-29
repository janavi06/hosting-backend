namespace Restaurant_Menu.Models
{
    // In your Models folder
    public class WaiterNotification
    {
        public int NotificationId { get; set; }
        public int OrderId { get; set; }
        public int TableNo { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsAcknowledged { get; set; }
        public int RestaurantID { get; set; }
        public Restaurant? Restaurant { get; set; }

        // Navigation property
        public Order Order { get; set; }
    }
}
