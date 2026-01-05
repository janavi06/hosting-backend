public class RestaurantPrinter
{
    public int Id { get; set; }
    public int RestaurantID { get; set; }
    public string PrinterType { get; set; } // KOT / BILL
    public string PrinterName { get; set; }
    public string HeaderText { get; set; }
    public string Address { get; set; }
    public string FooterText { get; set; }
    public bool IsActive { get; set; }
}
