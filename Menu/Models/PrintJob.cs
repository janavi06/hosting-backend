namespace Restaurant_Menu.Models
{
    public class PrintJob
    {
        public int PrintJobID { get; set; }
        public int RestaurantID { get; set; }
        public string PayloadJson { get; set; }
        public string Status { get; set; } = "PENDING";
        public DateTime CreatedAt { get; set; }
        public DateTime? PrintedAt { get; set; }
    }

}
