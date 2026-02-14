using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IOrderRepository
{
    Task<IEnumerable<Order>> GetAllOrdersAsync(int restaurantId);
    Task<Order> GetOrderByIdAsync(int orderId, int restaurantId);
    Task<Order> AddOrderAsync(Order order);
    Task<Order> GetOrderByIdWithItemsAsync(int orderId, int restaurantId);
    Task<Order> UpdateOrderAsync(Order order);
    Task<bool> DeleteOrderAsync(int orderId, int restaurantId);
    Task<bool> UpdateKitchenStatusAsync(int orderId, KitchenStatus status);
    Task<IEnumerable<Order>> GetPendingOrdersAsync(int restaurantId);
    Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status);
    Task<List<Payment>> GetPendingPaymentsAsync(int restaurantId);
    Task ApplyBestAvailableOfferAsync(Order order);
    Task<bool> AssignWaiterToOrderAsync(int orderId, int waiterUserId);
    Task<int?> GetNextAvailableWaiterAsync(int restaurantId);
    void CalculateOrderAmounts(Order order);
    Task CreateKitchenNotificationAsync(int orderId, int tableNo);
    Task<List<KitchenNotification>> GetUnacknowledgedNotificationsAsync(int restaurantId);
    Task AcknowledgeNotificationAsync(int notificationId);
    Task<WaiterRequest> AddWaiterRequestAsync(WaiterRequest request);
    Task<IEnumerable<Order>> GetOrdersWithDetailsAsync(int restaurantId);
    Task<Order> AddItemToOrderAsync(int orderId, OrderItem orderItem);
    Task<Order> RemoveItemFromOrderAsync(int orderId, int productId);

    Task<Order> UpdateOrderWithoutTrackingAsync(Order order); // ✅ ADD THIS
    Task<bool> ApplySpecificOfferAsync(Order order, int offerId);

}
