using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public void CalculateOrderAmounts(Order order)
    {

        foreach (var item in order.OrderItems)
        {
            item.RestaurantID = order.RestaurantID; // ✅ Ensures every item gets the same restaurant
        }

        // Subtotal: Quantity * UnitPrice for each item
        order.Subtotal = order.OrderItems.Sum(item => item.Quantity * item.UnitPrice);

        order.CGST = 0;
        order.SGST = 0;
        order.ServiceCharge = 0;

        order.DiscountAmount = 0;
        order.AppliedOfferID = null;

        if (order.RestaurantTableID > 0)
        {
            var table = _context.RestaurantTables
                .Include(t => t.Restaurant)
                .FirstOrDefault(t => t.RestaurantTableID == order.RestaurantTableID);

            if (table?.Restaurant != null)
            {
                var today = DateTime.UtcNow;

                var offer = _context.Offers
                    .Where(o =>
                        o.RestaurantID == table.Restaurant.RestaurantID &&
                        o.IsActive &&
                        o.AutoApply &&
                        o.ValidFrom <= today &&
                        o.ValidTo >= today &&
                        order.Subtotal >= o.MinBillAmount)
                    .OrderByDescending(o => o.MinBillAmount)
                    .FirstOrDefault();

                if (offer != null)
                {
                    decimal discount = 0;

                    if (offer.DiscountAmount.HasValue)
                    {
                        discount = offer.DiscountAmount.Value;
                    }
                    else if (offer.DiscountPercent.HasValue)
                    {
                        discount = (decimal)(offer.DiscountPercent.Value / 100f) * order.Subtotal;
                    }

                    order.DiscountAmount = discount;
                    order.AppliedOfferID = offer.OfferID;
                }
            }
        }

        order.TotalAmount = order.Subtotal - order.DiscountAmount;
    }





    // ✅ Get all orders
    public async Task<IEnumerable<Order>> GetAllOrdersAsync(int restaurantId)
    {
        return await _context.Orders
            .Where(o => o.RestaurantID == restaurantId)
            .Include(o => o.OrderItems)
            .ToListAsync();
    }


    // ✅ Get order by ID
    public async Task<Order?> GetOrderByIdAsync(int orderId, int restaurantId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);
    }


    // ✅ Add new order - UPDATED VERSION
    public async Task<Order> AddOrderAsync(Order order)
    {
        // Set audit and default values.
        order.CreatedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;
        order.CreatedBy ??= "System";
        order.UpdatedBy ??= "System";
        // Initially, set tax and service charge to 0.
        order.CGST = 0;
        order.SGST = 0;
        order.ServiceCharge = 0;
        order.OrderStatus = OrderStatus.Pending;
        order.KitchenStatus = KitchenStatus.Pending;

        // ✅ CRITICAL FIX: Ensure all order items and their customizations have the correct RestaurantID
        foreach (var item in order.OrderItems)
        {
            item.RestaurantID = order.RestaurantID; // Set restaurant ID for order item

            // ✅ Set restaurant ID for each customization
            foreach (var customization in item.Customizations)
            {
                customization.RestaurantID = order.RestaurantID;
            }
        }

        // Calculate order amounts based on order items
        CalculateOrderAmounts(order);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return order;
    }



    // ✅ Get order by ID with items (updated to return the full tracked entity)
    public async Task<Order?> GetOrderByIdWithItemsAsync(int orderId, int restaurantId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Customizations)
                    .ThenInclude(oic => oic.CustomizationOption)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.AppliedOffer) // Don't forget to include the offer
            .FirstOrDefaultAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);
    }


    public async Task<Order> UpdateOrderAsync(Order order)
    {
        // Assume 'order' is already tracked.
        order.UpdatedAt = DateTime.UtcNow;
        order.UpdatedBy = order.UpdatedBy ?? "System";

        // Recalculate the amounts after update.
        CalculateOrderAmounts(order);

        await _context.SaveChangesAsync();

        return order;
    }


    public async Task<IEnumerable<Order>> GetPendingOrdersAsync(int restaurantId)
    {
        return await _context.Orders
            .Where(o => (o.OrderStatus == OrderStatus.Pending || o.OrderStatus == OrderStatus.Confirmed) && o.RestaurantID == restaurantId)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Customizations)
                    .ThenInclude(oc => oc.CustomizationOption)
            .Include(o => o.Waiter)
            .ToListAsync();
    }





    // ✅ Get pending kitchen orders
    public async Task<List<Order>> GetPendingKitchenOrdersAsync()
    {
        return await _context.Orders
            .Where(o => o.KitchenStatus == KitchenStatus.Pending || o.KitchenStatus == KitchenStatus.Preparing)
            .Include(o => o.OrderItems)
            .ToListAsync();
    }

    // ✅ Update kitchen status
    public async Task<bool> UpdateKitchenStatusAsync(int orderId, KitchenStatus status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            Console.WriteLine($"❌ Order not found: {orderId}");
            return false;
        }

        Console.WriteLine($"✅ Updating Order {orderId} to Status: {status}");
        order.KitchenStatus = status;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    // ✅ Update order status
    public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return false;

        order.OrderStatus = status;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    // ✅ Delete an order
    public async Task<bool> DeleteOrderAsync(int orderId, int restaurantId)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);
        if (order == null) return false;

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return true;
    }


    // ✅ Add item to an order (modified version)
    public async Task<Order> AddItemToOrderAsync(int orderId, OrderItem orderItem)
    {
        // Look up the order using the OrderID (primary key)
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderID == orderId);

        if (order == null)
        {
            throw new Exception($"Order with ID {orderId} not found.");
        }

        // ✅ Reset kitchen status if it was marked as Ready
        if (order.KitchenStatus == KitchenStatus.Ready)
        {
            order.KitchenStatus = KitchenStatus.Preparing;
            Console.WriteLine($"🔁 KitchenStatus reset to Preparing for Order #{order.OrderID} due to item addition.");
        }

        // Add the item
        orderItem.OrderID = order.OrderID;
        order.OrderItems.Add(orderItem);

        // Update audit fields
        order.UpdatedAt = DateTime.UtcNow;
        order.UpdatedBy = order.UpdatedBy ?? "System";

        // ✅ Recalculate offers and totals
        CalculateOrderAmounts(order);
        await _context.SaveChangesAsync();

        return order;
    }



    // ✅ Assign waiter to an order (updated to set IsAssigned)
    public async Task<bool> AssignWaiterToOrderAsync(int orderId, int waiterUserId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return false;

        var waiter = await _context.Users.FindAsync(waiterUserId);
        if (waiter == null) return false;

        order.WaiterUserID = waiterUserId;
        order.IsAssigned = true; // Set the assignment flag on the order.
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<List<Payment>> GetPendingPaymentsAsync(int restaurantId)
    {
        return await _context.Payments
            .Include(p => p.Order)
                .ThenInclude(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
            .Where(p => p.PaymentStatus == PaymentStatus.Pending && p.Order.RestaurantID == restaurantId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetOrdersWithDetailsAsync(int restaurantId)
    {
        return await _context.Orders
            .Where(o => o.RestaurantID == restaurantId)
            .Include(o => o.Waiter)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .ToListAsync();
    }


    // ✅ Remove item from an order
    public async Task<Order> RemoveItemFromOrderAsync(int orderId, int productId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderID == orderId);

        if (order == null)
        {
            throw new Exception($"❌ Order with ID {orderId} not found!");
        }

        var itemToRemove = order.OrderItems.FirstOrDefault(oi => oi.ProductID == productId);
        if (itemToRemove != null)
        {
            order.OrderItems.Remove(itemToRemove);
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy ??= "System";

            CalculateOrderAmounts(order);


            await _context.SaveChangesAsync();
        }

        return order;
    }
    public async Task ApplyBestAvailableOfferAsync(Order order)
    {
        // Fetch active offers
        var offers = await _context.Offers
            .Where(o => o.IsActive && o.ValidFrom <= DateTime.UtcNow && o.ValidTo >= DateTime.UtcNow)
            .ToListAsync();

        Offer bestOffer = null;
        decimal maxDiscount = 0;

        foreach (var offer in offers)
        {
            decimal discount = 0;
            if (offer.DiscountPercent.HasValue)
            {
                discount = order.Subtotal * ((decimal)offer.DiscountPercent.Value / 100m);
            }
            else if (offer.DiscountAmount.HasValue)
            {
                discount = offer.DiscountAmount.Value;
            }

            if (discount > maxDiscount)
            {
                maxDiscount = discount;
                bestOffer = offer;
            }
        }

        if (bestOffer != null)
        {
            order.AppliedOffer = bestOffer;
            order.DiscountAmount = maxDiscount;
            order.TotalAmount = order.Subtotal + order.CGST + order.SGST + order.ServiceCharge - maxDiscount;
        }
        else
        {
            order.AppliedOffer = null;
            order.DiscountAmount = 0;
            order.TotalAmount = order.Subtotal + order.CGST + order.SGST + order.ServiceCharge;
        }

        _context.Orders.Update(order);
    }


    public async Task CreateKitchenNotificationAsync(int orderId, int tableNo)
    {
        var notification = new KitchenNotification
        {
            OrderId = orderId,
            TableNo = tableNo,
            Message = $"Order #{orderId} from Table {tableNo} is ready to serve"
        };

        _context.KitchenNotifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<List<KitchenNotification>> GetUnacknowledgedNotificationsAsync(int restaurantId)
    {
        return await _context.KitchenNotifications
            .Where(n => !n.IsAcknowledged && n.Order.RestaurantID == restaurantId)
            .Include(n => n.Order)
            .OrderBy(n => n.NotificationTime)
            .ToListAsync();
    }

    public async Task AcknowledgeNotificationAsync(int notificationId)
    {
        var notification = await _context.KitchenNotifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsAcknowledged = true;
            await _context.SaveChangesAsync();
        }
    }
    public async Task<WaiterRequest> AddWaiterRequestAsync(WaiterRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // Ensure the request time is set to the current time.
        request.RequestTime = DateTime.UtcNow;

        // Add the request to the database.
        _context.WaiterRequests.Add(request);
        await _context.SaveChangesAsync();

        return request;
    }

    public async Task<int?> GetNextAvailableWaiterAsync(int restaurantId)
    {
        List<User> waiters = await _context.Users
            .Where(u => u.UserRole == "Waiter" && u.IsAvailable && u.RestaurantID == restaurantId)
            .OrderBy(u => u.UserID)
            .ToListAsync();

        if (waiters.Count == 0) return null;

        var lastOrder = await _context.Orders
            .Where(o => o.WaiterUserID != null && o.RestaurantID == restaurantId)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (lastOrder?.WaiterUserID == null)
            return waiters.First().UserID;

        int lastWaiterId = lastOrder.WaiterUserID.Value;
        int index = waiters.FindIndex(w => w.UserID == lastWaiterId);
        return waiters[(index + 1) % waiters.Count].UserID;
    }

}



