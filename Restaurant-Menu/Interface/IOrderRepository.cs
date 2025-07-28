using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IOrderRepository
{
    Task<IEnumerable<Order>> GetAllOrdersAsync();
    Task<Order> GetOrderByIdAsync(int orderId);
    Task<Order> AddOrderAsync(Order order);
    Task<Order> GetOrderByIdWithItemsAsync(int orderId); // ✅ Include order items
    Task<Order> UpdateOrderAsync(Order order);
    Task<bool> DeleteOrderAsync(int orderId);
    Task<bool> UpdateKitchenStatusAsync(int orderId, KitchenStatus status);

    // ✅ Updated methods for Kitchen Orders
    Task<IEnumerable<Order>> GetPendingOrdersAsync();
    Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status); // ✅ Changed string to OrderStatus enum

    Task<List<Payment>> GetPendingPaymentsAsync();
    Task ApplyBestAvailableOfferAsync(Order order);


    Task<bool> AssignWaiterToOrderAsync(int orderId, int waiterUserId);

    Task<int?> GetNextAvailableWaiterAsync();


    // Add the calculation method signature.
    void CalculateOrderAmounts(Order order);


    Task CreateKitchenNotificationAsync(int orderId, int tableNo);
    Task<List<KitchenNotification>> GetUnacknowledgedNotificationsAsync();
    Task AcknowledgeNotificationAsync(int notificationId);
    Task<WaiterRequest> AddWaiterRequestAsync(WaiterRequest request);
    Task<IEnumerable<Order>> GetOrdersWithDetailsAsync();

}
