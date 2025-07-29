using Microsoft.AspNetCore.Mvc;
using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;

[Route("api/[controller]")]
[ApiController]
public class OrderItemController : ControllerBase
{
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly ILogger<OrderItemController> _logger;

    public OrderItemController(
        IOrderItemRepository orderItemRepository,
        ILogger<OrderItemController> logger)
    {
        _orderItemRepository = orderItemRepository;
        _logger = logger;
    }

    // ✅ Get all order items by OrderID (Restaurant-level scoped)
    [HttpGet("order/{orderId}")]
    public async Task<ActionResult<IEnumerable<OrderItem>>> GetOrderItemsByOrderId(int orderId, [FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("RestaurantID is required.");

        _logger.LogInformation($"Fetching order items for OrderID={orderId} and RestaurantID={restaurantId}");

        var orderItems = await _orderItemRepository.GetOrderItemsByOrderIdAsync(orderId);
        if (orderItems == null || !orderItems.Any())
        {
            return NotFound($"No order items found for OrderID={orderId}");
        }

        // ✅ Ensure filtering
        var filteredItems = orderItems.Where(x => x.RestaurantID == restaurantId).ToList();

        return Ok(filteredItems);
    }

    // ✅ Add multiple order items to an Order
    [HttpPost("order/{orderId}")]
    public async Task<ActionResult> AddOrderItemsToOrder(int orderId, [FromQuery] int restaurantId, [FromBody] List<OrderItem> orderItems)
    {
        if (restaurantId <= 0)
            return BadRequest("RestaurantID is required.");

        if (orderItems == null || !orderItems.Any())
            return BadRequest("No items provided.");

        foreach (var item in orderItems)
        {
            item.OrderID = orderId;
            item.RestaurantID = restaurantId;
            if (string.IsNullOrEmpty(item.CreatedBy)) item.CreatedBy = "DefaultUser";
            if (string.IsNullOrEmpty(item.UpdatedBy)) item.UpdatedBy = item.CreatedBy;
        }

        try
        {
            await _orderItemRepository.AddOrderItemsAsync(orderItems, orderId);
            return Ok(new { message = "Order items added successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add batch order items.");
            return StatusCode(500, "Failed to add batch order items.");
        }
    }

    // ✅ Update an order item inside an order
    [HttpPut("order/{orderId}/item/{id}")]
    public async Task<IActionResult> UpdateOrderItem(int orderId, int id, [FromQuery] int restaurantId, [FromBody] OrderItem orderItem)
    {
        if (restaurantId <= 0)
            return BadRequest("RestaurantID is required.");

        if (id != orderItem.OrderItemID || orderId != orderItem.OrderID)
            return BadRequest("Mismatched OrderItem or OrderID.");

        var existingItem = await _orderItemRepository.GetOrderItemByIdAsync(id);
        if (existingItem == null || existingItem.RestaurantID != restaurantId)
            return NotFound($"OrderItem ID={id} not found for restaurant.");

        try
        {
            existingItem.UpdatedBy = "DefaultUser";
            existingItem.UpdatedAt = DateTime.UtcNow;

            await _orderItemRepository.UpdateOrderItemAsync(orderItem);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order item.");
            return StatusCode(500, "Failed to update order item.");
        }
    }

    // ✅ Delete an order item inside an order
    [HttpDelete("order/{orderId}/item/{id}")]
    public async Task<IActionResult> DeleteOrderItem(int orderId, int id, [FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("RestaurantID is required.");

        _logger.LogInformation($"Attempting to delete OrderItem ID={id} from OrderID={orderId} for RestaurantID={restaurantId}");

        var orderItem = await _orderItemRepository.GetOrderItemByIdAsync(id);
        if (orderItem == null || orderItem.OrderID != orderId || orderItem.RestaurantID != restaurantId)
        {
            return NotFound($"OrderItem ID={id} not found for OrderID={orderId} and RestaurantID={restaurantId}.");
        }

        try
        {
            await _orderItemRepository.DeleteOrderItemAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete order item.");
            return StatusCode(500, "Failed to delete order item.");
        }
    }
}
