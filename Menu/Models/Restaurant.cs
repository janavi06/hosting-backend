namespace Restaurant_Menu.Models
{
    public class Restaurant
    {
        public int RestaurantID { get; set; }
        public string Name { get; set; } = "";
        public string? Address { get; set; } // Make sure this exists

        public string? Description { get; set; }
        public string? LogoPath { get; set; }
        public string? KotPrinterName { get; set; }
        public string? BillPrinterName { get; set; }
        public string? LocalPrintServiceUrl { get; set; }
        public string UPI_ID { get; set; } = "";
        public string UPI_Name { get; set; } = "";


    }

}
