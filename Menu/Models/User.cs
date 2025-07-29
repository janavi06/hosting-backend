namespace Restaurant_Menu.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string UserRole { get; set; }
        public string? UserName { get; set; } // Only required for staff (Waiters/Admins)
        public string? PhoneNumber { get; set; } // Required for customers (easy access)

        // Only for Staff (Waiters/Admins)
        public string? Email { get; set; } // Only needed for staff login (not for customers)
        public string? PasswordHash { get; set; } // Only needed for staff authentication

        public string? CreatedBy { get; set; } = "System";  // Default value
        public string? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsAvailable { get; set; } = true; // For assignment purposes
        public int RestaurantID { get; set; }
        public Restaurant? Restaurant { get; set; }

        // Navigation property for orders assigned to the waiter
        public ICollection<Order>? AssignedOrders { get; set; }
    }
}
