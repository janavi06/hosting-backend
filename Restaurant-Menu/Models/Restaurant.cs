namespace Restaurant_Menu.Models
{
    public class Restaurant
    {
        public int RestaurantID { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? LogoPath { get; set; }

        public string UPI_ID { get; set; } = "";
        public string UPI_Name { get; set; } = "";


    }

}
