
using System;

namespace Restaurant_Menu.Models
{
    // WaiterRequest.cs
    public class WaiterRequest
    {
        public int WaiterRequestID { get; set; }
        public string Message { get; set; }
        public int? TableNumber { get; set; }
        public DateTime RequestTime { get; set; }
        public bool IsNotified { get; set; } = false; // New field to track notification status
    }
}
