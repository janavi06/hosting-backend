using Restaurant_Menu.Models;

public interface IOrderItemRepository
{
    Task<IEnumerable<OrderItem>> GetAllOrderItemsAsync();
    Task<IEnumerable<OrderItem>> GetOrderItemsByOrderIdAsync(int orderId);
    Task<OrderItem> GetOrderItemByIdAsync(int orderItemId);
    Task<OrderItem> AddOrderItemAsync(OrderItem orderItem);
    Task AddOrderItemsAsync(ICollection<OrderItem> orderItems, int orderId);
    Task<OrderItem> UpdateOrderItemAsync(OrderItem orderItem);
    Task<bool> DeleteOrderItemAsync(int orderItemId);
    Task<Order> GetOrderWithItemsAsync(int orderId);
}
