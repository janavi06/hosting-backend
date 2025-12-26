namespace Restaurant_Menu.Models
{
    public class RestaurantTable
    {
        public int RestaurantTableID { get; set; }
        public string? TableName { get; set; }
        public int? Seats { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }


        public int TableNo { get; set; }


        // ✅ New: RestaurantID Link
        public int RestaurantID { get; set; }
        public Restaurant? Restaurant { get; set; }  // Navigation Property
        public ICollection<Order> Orders { get; set; }
    }
}
