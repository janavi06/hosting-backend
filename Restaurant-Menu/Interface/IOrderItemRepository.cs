using Restaurant_Menu.Models;

public interface IOrderItemRepository
{
    Task<IEnumerable<OrderItem>> GetAllOrderItemsAsync();
    Task<OrderItem> GetOrderItemByIdAsync(int orderItemId);
    Task<OrderItem> AddOrderItemAsync(OrderItem orderItem);
    Task<OrderItem> UpdateOrderItemAsync(OrderItem orderItem);
    Task AddOrderItemsAsync(ICollection<OrderItem> orderItems, int orderId); // Change here
    Task<IEnumerable<OrderItem>> GetOrderItemsByOrderIdAsync(int orderId); // Add this
    Task<bool> DeleteOrderItemAsync(int orderItemId);

    // Add the new method here
    Task<Order> GetOrderWithItemsAsync(int orderId);  // This is the missing method
}
