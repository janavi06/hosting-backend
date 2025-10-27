// DTOs/PrintDtos.cs
using System.Collections.Generic;

namespace Restaurant_Menu.DTOs
{
    public class PrintRequestDto
    {
        public string Type { get; set; } // "KOT" or "BILL"
        public string PrinterName { get; set; } // optional override (exact Windows printer name)
        public string KotPrinterName { get; set; } // fallback from restaurant record
        public string BillPrinterName { get; set; } // fallback from restaurant record
        public string RestaurantName { get; set; }
        public string RestaurantAddress { get; set; }
        public PrintOrderDto Order { get; set; }
    }

    public class PrintOrderDto
    {
        public string OrderNumber { get; set; }
        public string TableNo { get; set; }
        public List<PrintItemDto> Items { get; set; } = new List<PrintItemDto>();
        public decimal? ServiceCharge { get; set; }
        public decimal? Tax { get; set; }
        public decimal? Discount { get; set; }
        public decimal? Total { get; set; }
        public string Notes { get; set; }
    }

    public class PrintItemDto
    {
        public string Name { get; set; }
        public int Qty { get; set; }
        public decimal Price { get; set; }
        public List<string> Modifiers { get; set; } = new List<string>();
    }
}
