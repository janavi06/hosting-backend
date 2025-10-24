using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class OrderItemRepository : IOrderItemRepository
{
    private readonly ApplicationDbContext _context;

    public OrderItemRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    private async Task AdjustInventoryForProductAsync(int restaurantId, int productId, int orderId, int quantityDelta, string createdBy)
    {
        // quantityDelta > 0 means sell; < 0 means revert
        var recipes = await _context.ProductRecipes
            .Where(r => r.ProductID == productId && r.RestaurantID == restaurantId)
            .ToListAsync();

        foreach (var recipe in recipes)
        {
            var item = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.InventoryItemID == recipe.InventoryItemID && i.RestaurantID == restaurantId);
            if (item == null) continue;

            var qtyChange = -(recipe.QuantityPerUnit * quantityDelta); // sale reduces stock
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

    public async Task<IEnumerable<OrderItem>> GetAllOrderItemsAsync()
    {
        return await _context.OrderItems
            .Include(oi => oi.Customizations)
            .ThenInclude(oic => oic.CustomizationOption)
            .ToListAsync();
    }

    public async Task<IEnumerable<OrderItem>> GetOrderItemsByOrderIdAsync(int orderId)
    {
        return await _context.OrderItems
            .Where(oi => oi.OrderID == orderId)
            .Include(oi => oi.Customizations)
            .ThenInclude(oic => oic.CustomizationOption)
            .ToListAsync();
    }

    public async Task<OrderItem> GetOrderItemByIdAsync(int orderItemId)
    {
        return await _context.OrderItems
            .Include(oi => oi.Customizations)
            .ThenInclude(oic => oic.CustomizationOption)
            .FirstOrDefaultAsync(oi => oi.OrderItemID == orderItemId);
    }

    public async Task<OrderItem> AddOrderItemAsync(OrderItem orderItem)
    {
        await ValidateOrderAndProduct(orderItem.OrderID, orderItem.ProductID);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            orderItem.CreatedAt = DateTime.UtcNow;
            orderItem.UpdatedAt = DateTime.UtcNow;
            orderItem.CreatedBy ??= "DefaultUser";
            orderItem.UpdatedBy ??= orderItem.CreatedBy;

            _context.OrderItems.Add(orderItem);
            await _context.SaveChangesAsync();

            await AdjustInventoryForProductAsync(orderItem.RestaurantID, orderItem.ProductID, orderItem.OrderID, orderItem.Quantity, orderItem.CreatedBy!);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return orderItem;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task AddOrderItemsAsync(ICollection<OrderItem> orderItems, int orderId)
    {
        if (orderItems == null || !orderItems.Any())
            throw new ArgumentException("Order items list cannot be empty.");

        await ValidateOrderExists(orderId);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in orderItems)
            {
                await ValidateProductExists(item.ProductID);

                item.OrderID = orderId;
                item.CreatedAt = DateTime.UtcNow;
                item.UpdatedAt = DateTime.UtcNow;
                item.CreatedBy ??= "DefaultUser";
                item.UpdatedBy ??= item.CreatedBy;

                _context.OrderItems.Add(item);
            }

            await _context.SaveChangesAsync();

            // Adjust inventory for each item
            foreach (var item in orderItems)
            {
                await AdjustInventoryForProductAsync(item.RestaurantID, item.ProductID, item.OrderID, item.Quantity, item.CreatedBy!);
            }
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<OrderItem> UpdateOrderItemAsync(OrderItem orderItem)
    {
        var existingItem = await _context.OrderItems.FindAsync(orderItem.OrderItemID);
        if (existingItem == null)
            throw new KeyNotFoundException($"OrderItem ID {orderItem.OrderItemID} not found.");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var actor = orderItem.UpdatedBy ?? "DefaultUser";
            // If product changed, revert old and apply new
            if (orderItem.ProductID != existingItem.ProductID)
            {
                await AdjustInventoryForProductAsync(existingItem.RestaurantID, existingItem.ProductID, existingItem.OrderID, -existingItem.Quantity, actor);
                await AdjustInventoryForProductAsync(orderItem.RestaurantID, orderItem.ProductID, orderItem.OrderID, orderItem.Quantity, actor);
            }
            else
            {
                var delta = orderItem.Quantity - existingItem.Quantity;
                if (delta != 0)
                {
                    await AdjustInventoryForProductAsync(existingItem.RestaurantID, existingItem.ProductID, existingItem.OrderID, delta, actor);
                }
            }

            // Update fields
            existingItem.ProductID = orderItem.ProductID;
            existingItem.Quantity = orderItem.Quantity;
            existingItem.UnitPrice = orderItem.UnitPrice;
            existingItem.UpdatedBy = actor;
            existingItem.UpdatedAt = DateTime.UtcNow;

            _context.OrderItems.Update(existingItem);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return existingItem;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteOrderItemAsync(int orderItemId)
    {
        var orderItem = await _context.OrderItems.FindAsync(orderItemId);
        if (orderItem == null) return false;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Revert inventory for this item
            await AdjustInventoryForProductAsync(orderItem.RestaurantID, orderItem.ProductID, orderItem.OrderID, -orderItem.Quantity, orderItem.UpdatedBy ?? "System");

            _context.OrderItems.Remove(orderItem);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Order> GetOrderWithItemsAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderID == orderId);

        if (order == null)
            throw new KeyNotFoundException($"Order with ID {orderId} not found.");

        return order;
    }

    // 🔹 Validation Helpers
    private async Task ValidateOrderAndProduct(int orderId, int productId)
    {
        await ValidateOrderExists(orderId);
        await ValidateProductExists(productId);
    }

    private async Task ValidateOrderExists(int orderId)
    {
        if (!await _context.Orders.AnyAsync(o => o.OrderID == orderId))
            throw new KeyNotFoundException($"Order ID {orderId} not found.");
    }

    private async Task ValidateProductExists(int productId)
    {
        if (!await _context.Products.AnyAsync(p => p.ProductID == productId))
            throw new KeyNotFoundException($"Product ID {productId} not found.");
    }
}
