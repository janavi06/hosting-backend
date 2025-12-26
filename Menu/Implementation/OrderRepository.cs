using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(ApplicationDbContext context, ILogger<OrderRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    private async Task AdjustInventoryForProductAsync(int restaurantId, int productId, int orderId, int quantityDelta, string createdBy)
    {
        var recipes = await _context.ProductRecipes
            .Where(r => r.ProductID == productId && r.RestaurantID == restaurantId)
            .ToListAsync();

        foreach (var recipe in recipes)
        {
            var item = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.InventoryItemID == recipe.InventoryItemID && i.RestaurantID == restaurantId);
            if (item == null) continue;

            var qtyChange = -(recipe.QuantityPerUnit * quantityDelta);
            item.CurrentQuantity += qtyChange;
            item.UpdatedAt = DateTime.UtcNow;
            item.UpdatedBy = createdBy;

            _context.StockTransactions.Add(new StockTransaction
            {
                InventoryItemID = recipe.InventoryItemID,
                RestaurantID = restaurantId,
                TransactionType = quantityDelta >= 0 ? StockTransactionType.Sale : StockTransactionType.Return,
                QuantityChange = qtyChange,
                UnitCost = item.AverageUnitCost,
                Reference = $"order:{orderId}",
                Notes = quantityDelta >= 0 ? "Order sale deduction" : "Order item revert",
                CreatedBy = createdBy,
                TransactionTime = DateTime.UtcNow
            });
        }
    }

    
    public async Task<IEnumerable<Order>> GetAllOrdersAsync(int restaurantId)
    {
        return await _context.Orders
            .Where(o => o.RestaurantID == restaurantId)
            .Include(o => o.OrderItems)
            .ToListAsync();
    }

    // In OrderRepository.cs

    public void CalculateOrderAmounts(Order order)
    {
        if (order.OrderItems == null)
        {
            order.Subtotal = 0;
            order.TotalAmount = 0;
            return;
        }

        decimal currentSubtotal = 0;

        foreach (var item in order.OrderItems)
        {
            // Fetch the base product from the database
            var product = _context.Products
                .AsNoTracking()
                .FirstOrDefault(p => p.ProductID == item.ProductID);

            if (product == null) continue;

            decimal basePrice = product.Price;
            decimal customizationPrice = 0;

            // ✅ NEW ROBUST LOGIC:
            // Instead of relying on navigation properties, we fetch prices
            // directly from the database using the IDs we already have.
            if (item.Customizations != null && item.Customizations.Any())
            {
                // Get all the Option IDs from the current item
                var optionIds = item.Customizations
                                    .Select(c => c.CustomizationOptionID)
                                    .ToList();

                customizationPrice = item.Customizations.Sum(c =>
      c.CustomizationOption?.FixedPrice ?? 0m); // Use 0m for decimal
            }

            // Calculate the total unit price including customizations
            decimal totalUnitPrice = basePrice + customizationPrice;

            // Update the item's unit price to include customizations
            item.UnitPrice = totalUnitPrice;

            // Add to running subtotal
            currentSubtotal += item.Quantity * totalUnitPrice;

            Console.WriteLine($"💰 Item {item.ProductID}: Base={basePrice}, Customizations={customizationPrice}, TotalUnit={totalUnitPrice}, Qty={item.Quantity}, LineTotal={item.Quantity * totalUnitPrice}");
        }

        order.Subtotal = currentSubtotal;
        Console.WriteLine($"📊 Order {order.OrderID} Subtotal: {order.Subtotal}");

        // Apply discount if any
        order.DiscountAmount = order.DiscountAmount;

        // Calculate taxes and service charge (uncomment if needed)
        // order.CGST = order.Subtotal * 0.025m; // 2.5%
        // order.SGST = order.Subtotal * 0.025m; // 2.5%
        // order.ServiceCharge = order.Subtotal * 0.05m; // 5%

        // Calculate final total
        order.TotalAmount = order.Subtotal + order.CGST + order.SGST + order.ServiceCharge - order.DiscountAmount;

        Console.WriteLine($"🎯 Order {order.OrderID} Final Total: {order.TotalAmount}");
    }
    // ✅ Get order by ID
    public async Task<Order?> GetOrderByIdAsync(int orderId, int restaurantId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);
    }
    // ✅ NEW: Helper method to get next order number for a restaurant
private async Task<int> GetNextOrderNumberAsync(int restaurantId)
{
    var lastOrder = await _context.Orders
        .Where(o => o.RestaurantID == restaurantId)
        .OrderByDescending(o => o.OrderNumber)
        .FirstOrDefaultAsync();

    return (lastOrder?.OrderNumber ?? 0) + 1;
}

    // ✅ Add new order - UPDATED VERSION
    public async Task<Order> AddOrderAsync(Order order)
    {
        // Set audit and default values.
        order.CreatedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;
        order.CreatedBy ??= "System";
        order.UpdatedBy ??= "System";
        order.CGST = 0;
        order.SGST = 0;
        order.ServiceCharge = 0;
        order.OrderStatus = OrderStatus.Pending;
        order.KitchenStatus = KitchenStatus.Pending;

        // ✅ NEW: If OrderNumber is not set, get the next one for the restaurant
        if (order.OrderNumber == 0)
        {
            order.OrderNumber = await GetNextOrderNumberAsync(order.RestaurantID);
        }

        // ✅ FIX: Handle customizations properly - don't create new CustomizationOption entities
        foreach (var item in order.OrderItems)
        {
            item.RestaurantID = order.RestaurantID;

            // ✅ FIX: Ensure we're only setting IDs for existing customization options
            if (item.Customizations != null && item.Customizations.Any())
            {
                var validCustomizations = new List<OrderItemCustomization>();

                foreach (var customization in item.Customizations)
                {
                    // ✅ Check if the customization option exists
                    var existingOption = await _context.CustomizationOptions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(co => co.CustomizationOptionID == customization.CustomizationOptionID);

                    if (existingOption != null)
                    {
                        // ✅ Only add the relationship, don't create new CustomizationOption
                        validCustomizations.Add(new OrderItemCustomization
                        {
                            CustomizationOptionID = customization.CustomizationOptionID,
                            RestaurantID = order.RestaurantID
                            // ✅ Don't set the navigation property to avoid tracking issues
                        });
                    }
                }

                // ✅ Replace with valid customizations
                item.Customizations = validCustomizations;
            }
        }

        // First add and save the order → ensures OrderID is generated
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Adjust inventory for all order items (recipe-based deduction)
        foreach (var item in order.OrderItems)
        {
            await AdjustInventoryForProductAsync(order.RestaurantID, item.ProductID, order.OrderID, item.Quantity, order.CreatedBy ?? "System");
        }
        await _context.SaveChangesAsync();

        // Now calculate totals and apply offers
        CalculateOrderAmounts(order);

        // Save again for totals and applied offer
        await _context.SaveChangesAsync();

        return order;
    }

    public async Task<Order?> GetOrderByIdWithItemsAsync(int orderId, int restaurantId)
    {
        return await _context.Orders
            // .AsNoTracking() // ✅ REMOVE THIS LINE
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product) // Include Product
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Customizations)
                .ThenInclude(c => c.CustomizationOption) // Include Customizations
            .Include(o => o.AppliedOffer)
            .FirstOrDefaultAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);
    }

    public async Task<Order> UpdateOrderWithoutTrackingAsync(Order order)
    {
        try
        {
            // ✅ FIX: Use a completely fresh approach - find the order without tracking
            var existingOrder = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Customizations)
                .FirstOrDefaultAsync(o => o.OrderID == order.OrderID);

            if (existingOrder == null)
            {
                throw new Exception($"Order with ID {order.OrderID} not found.");
            }

            // Update basic order properties
            existingOrder.UpdatedAt = DateTime.UtcNow;
            existingOrder.UpdatedBy = order.UpdatedBy ?? "System";
            existingOrder.OrderStatus = order.OrderStatus;
            existingOrder.KitchenStatus = order.KitchenStatus;
            existingOrder.Subtotal = order.Subtotal;
            existingOrder.DiscountAmount = order.DiscountAmount;
            existingOrder.CGST = order.CGST;
            existingOrder.SGST = order.SGST;
            existingOrder.ServiceCharge = order.ServiceCharge;
            existingOrder.TotalAmount = order.TotalAmount;
            existingOrder.AppliedOfferID = order.AppliedOfferID;

            // ✅ NEW: Preserve OrderNumber
            existingOrder.OrderNumber = order.OrderNumber;

            // ✅ FIX: Clear existing items and add new ones
            existingOrder.OrderItems.Clear();

            foreach (var newItem in order.OrderItems)
            {
                var orderItem = new OrderItem
                {
                    ProductID = newItem.ProductID,
                    Quantity = newItem.Quantity,
                    UnitPrice = newItem.UnitPrice,
                    IsPrepared = newItem.IsPrepared,
                    AddedToKitchenAt = newItem.AddedToKitchenAt,
                    PreparedAt = newItem.PreparedAt,
                    BatchID = newItem.BatchID,
                    RestaurantID = newItem.RestaurantID,
                    Customizations = new List<OrderItemCustomization>()
                };

                // Add customizations - only set the ID, not the navigation property
                if (newItem.Customizations != null && newItem.Customizations.Any())
                {
                    foreach (var customization in newItem.Customizations)
                    {
                        orderItem.Customizations.Add(new OrderItemCustomization
                        {
                            CustomizationOptionID = customization.CustomizationOptionID,
                            RestaurantID = customization.RestaurantID
                        });
                    }
                }

                existingOrder.OrderItems.Add(orderItem);
            }

            await _context.SaveChangesAsync();

            // Return the updated order by fetching it fresh
            return await GetOrderByIdWithItemsAsync(order.OrderID, order.RestaurantID);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in UpdateOrderWithoutTrackingAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<Order> UpdateOrderAsync(Order order)
    {
        try
        {
            // ✅ FIX: Use a fresh approach to avoid tracking conflicts
            var existingOrder = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Customizations)
                .FirstOrDefaultAsync(o => o.OrderID == order.OrderID);

            if (existingOrder == null)
            {
                throw new Exception($"Order with ID {order.OrderID} not found.");
            }

            _context.ChangeTracker.Entries<CustomizationOption>().ToList()
                .ForEach(entry => entry.State = EntityState.Detached);

            existingOrder.UpdatedAt = DateTime.UtcNow;
            existingOrder.UpdatedBy = order.UpdatedBy ?? "System";
            existingOrder.OrderStatus = order.OrderStatus;
            existingOrder.KitchenStatus = order.KitchenStatus;
            existingOrder.Subtotal = order.Subtotal;
            existingOrder.DiscountAmount = order.DiscountAmount;
            existingOrder.CGST = order.CGST;
            existingOrder.SGST = order.SGST;
            existingOrder.ServiceCharge = order.ServiceCharge;
            existingOrder.TotalAmount = order.TotalAmount;
            existingOrder.AppliedOfferID = order.AppliedOfferID;

            existingOrder.OrderNumber = order.OrderNumber;


            // Handle order items - clear and re-add to avoid complex tracking
            existingOrder.OrderItems.Clear();

            foreach (var newItem in order.OrderItems)
            {
                var orderItem = new OrderItem
                {
                    ProductID = newItem.ProductID,
                    Quantity = newItem.Quantity,
                    UnitPrice = newItem.UnitPrice,
                    IsPrepared = newItem.IsPrepared,
                    AddedToKitchenAt = newItem.AddedToKitchenAt,
                    PreparedAt = newItem.PreparedAt,
                    BatchID = newItem.BatchID,
                    RestaurantID = newItem.RestaurantID,
                    Customizations = new List<OrderItemCustomization>()
                };

                // Add customizations without tracking the CustomizationOption entities
                if (newItem.Customizations != null && newItem.Customizations.Any())
                {
                    foreach (var customization in newItem.Customizations)
                    {
                        orderItem.Customizations.Add(new OrderItemCustomization
                        {
                            CustomizationOptionID = customization.CustomizationOptionID,
                            RestaurantID = customization.RestaurantID
                            // ✅ Don't set the navigation property to avoid tracking
                        });
                    }
                }

                existingOrder.OrderItems.Add(orderItem);
            }

            await _context.SaveChangesAsync();
            return existingOrder;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in UpdateOrderAsync: {ex.Message}");
            throw;
        }
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
            .OrderByDescending(o => o.OrderNumber) 
            .ToListAsync();
    }


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
        }
        else
        {
            order.AppliedOffer = null;
            order.DiscountAmount = 0;
        }

        // Total recalculation handled by CalculateOrderAmounts
    }



    public async Task CreateKitchenNotificationAsync(int orderId, int tableNo)
    {
        // ✅ NEW: Get the order to access OrderNumber
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return;

        var notification = new KitchenNotification
        {
            OrderId = orderId,
            TableNo = tableNo,
            Message = $"Order #{order.OrderNumber} from Table {tableNo} is ready to serve" // ✅ Use OrderNumber
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