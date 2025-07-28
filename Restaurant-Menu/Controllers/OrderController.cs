

using DinkToPdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Restaurant_Menu.Models;
using Restaurant_System.Models;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.Threading.Tasks;
[ApiController]
[Route("api/order")]
public class OrderController : ControllerBase
{
    private readonly ApplicationDbContext _context; // Declare the context
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<OrderController> _logger;
    public OrderController(ApplicationDbContext context, IOrderRepository orderRepository, IProductRepository productRepository, IUserRepository userRepository, ILogger<OrderController> logger)
    {
        _context = context;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _logger = logger;

    }

    [HttpPost("generate")]
    public async Task<ActionResult<Order>> GenerateOrder(
      [FromQuery] int? tableNo, [FromBody] Order orderData)
    {
        if (tableNo.HasValue)
            orderData.RestaurantTableID = tableNo.Value;

        try
        {
            if (orderData == null)
            {
                orderData = new Order();
            }

            if (orderData.UserID <= 0)
            {
                var newCustomer = new User
                {
                    UserRole = "customer",
                    UserName = "Anonymous",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    IsAvailable = true
                };

                _context.Users.Add(newCustomer);
                await _context.SaveChangesAsync();
                orderData.UserID = newCustomer.UserID;
            }

            var newOrder = new Order
            {
                UserID = orderData.UserID,
                RestaurantTableID = orderData.RestaurantTableID,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = orderData.UserID > 0 ? orderData.UserID.ToString() : "System",
                UpdatedBy = orderData.UserID > 0 ? orderData.UserID.ToString() : "System",
                OrderStatus = OrderStatus.Pending,
                KitchenStatus = KitchenStatus.Pending,
                OrderItems = orderData.OrderItems ?? new List<OrderItem>(),
                IsAssigned = false
            };
            int? waiterId = await _orderRepository.GetNextAvailableWaiterAsync();
            if (waiterId.HasValue)
            {
                newOrder.WaiterUserID = waiterId.Value;
                newOrder.IsAssigned = true;
            }
            else
            {
                _logger.LogWarning("No available waiter found for automatic assignment.");
            }

            var createdOrder = await _orderRepository.AddOrderAsync(newOrder);

            return Ok(new
            {
                message = "Order generated with automatic waiter assignment!",
                orderID = createdOrder.OrderID,
                orderItems = createdOrder.OrderItems
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating order: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "An error occurred while generating the order.");
        }
    }



    [HttpPost("{orderId}/addItem")]
    public async Task<IActionResult> AddItemsToCart(int orderId, [FromBody] List<OrderItem> orderItems)
    {
        try
        {
            var existingOrder = await _orderRepository.GetOrderByIdWithItemsAsync(orderId);
            if (existingOrder == null)
                return NotFound(new { message = "Order not found." });

            // ❌ If the order is already completed or served, create a new order instead
            if (existingOrder.OrderStatus == OrderStatus.Completed ||
                existingOrder.OrderStatus == OrderStatus.Served)
            {
                var newOrder = new Order
                {
                    UserID = existingOrder.UserID,
                    RestaurantTableID = existingOrder.RestaurantTableID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    OrderStatus = OrderStatus.Pending,
                    KitchenStatus = KitchenStatus.Pending,
                    OrderItems = new List<OrderItem>()
                };

                // ✅ Assign waiter if available
                int? waiterId = await _orderRepository.GetNextAvailableWaiterAsync();
                if (waiterId.HasValue)
                {
                    newOrder.WaiterUserID = waiterId.Value;
                    newOrder.IsAssigned = true;
                }

                foreach (var incoming in orderItems)
                {
                    var product = await _productRepository.GetProductByIdAsync(incoming.ProductID);
                    if (product == null)
                        return NotFound(new { message = $"Product with ID {incoming.ProductID} not found." });

                    if (!product.IsAvailable)
                        return BadRequest(new { message = $"Product '{product.ProductName}' is currently not available." });

                    var newItem = new OrderItem
                    {
                        ProductID = incoming.ProductID,
                        Quantity = incoming.Quantity,
                        UnitPrice = incoming.UnitPrice,
                        IsPrepared = false,
                        AddedToKitchenAt = DateTime.UtcNow,
                        BatchID = 1,
                        Customizations = new List<OrderItemCustomization>()
                    };

                    if (incoming.CustomizationOptionIds != null)
                    {
                        foreach (var optId in incoming.CustomizationOptionIds)
                        {
                            newItem.Customizations.Add(new OrderItemCustomization
                            {
                                CustomizationOptionID = optId
                            });
                        }
                    }

                    newOrder.OrderItems.Add(newItem);
                }

                _orderRepository.CalculateOrderAmounts(newOrder);
                var createdOrder = await _orderRepository.AddOrderAsync(newOrder);

                return Ok(new
                {
                    message = "New order created for additional items!",
                    orderID = createdOrder.OrderID,
                    newItems = orderItems.Select(item => new
                    {
                        productID = item.ProductID,
                        quantity = item.Quantity
                    })
                });
            }

            // ✅ Reset kitchen status if previously Ready
            if (existingOrder.KitchenStatus == KitchenStatus.Ready)
            {
                existingOrder.KitchenStatus = KitchenStatus.Pending;
                // ❌ Do not reset LastKitchenReadyAt, so old items stay hidden
            }

            // ✅ Determine next BatchID
            int maxBatchId = existingOrder.OrderItems.Any()
                ? existingOrder.OrderItems.Max(oi => oi.BatchID)
                : 0;
            int newBatchId = maxBatchId + 1;

            foreach (var incoming in orderItems)
            {
                var product = await _productRepository.GetProductByIdAsync(incoming.ProductID);
                if (product == null)
                    return NotFound(new { message = $"Product with ID {incoming.ProductID} not found." });

                if (!product.IsAvailable)
                    return BadRequest(new { message = $"Product '{product.ProductName}' is currently not available." });

                var newItem = new OrderItem
                {
                    ProductID = incoming.ProductID,
                    Quantity = incoming.Quantity,
                    UnitPrice = incoming.UnitPrice,
                    IsPrepared = false,
                    AddedToKitchenAt = DateTime.UtcNow,
                    BatchID = newBatchId,
                    Customizations = new List<OrderItemCustomization>()
                };

                if (incoming.CustomizationOptionIds != null)
                {
                    foreach (var optId in incoming.CustomizationOptionIds)
                    {
                        newItem.Customizations.Add(new OrderItemCustomization
                        {
                            CustomizationOptionID = optId
                        });
                    }
                }

                existingOrder.OrderItems.Add(newItem);
            }

            _orderRepository.CalculateOrderAmounts(existingOrder);
            await _orderRepository.UpdateOrderAsync(existingOrder);

            // ✅ Reapply offers after recalculation
            await _orderRepository.ApplyBestAvailableOfferAsync(existingOrder);
            await _context.SaveChangesAsync();


            return Ok(new
            {
                message = "Items added to cart successfully!",
                orderID = existingOrder.OrderID,
                newItems = orderItems.Select(item => new
                {
                    productID = item.ProductID,
                    quantity = item.Quantity
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding items: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "An error occurred while adding items to the cart.");
        }
    }




    [HttpPost("{orderId}/updateSummary")]
    public async Task<IActionResult> UpdateOrderSummary(int orderId)
    {
        try
        {
            var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId);
            if (order == null || order.OrderItems == null || !order.OrderItems.Any())
            {
                return NotFound(new { message = "Cart is empty. Add items before proceeding." });
            }

            return Ok(new
            {
                message = "Order summary updated successfully!",
                orderID = order.OrderID,
                orderItems = order.OrderItems.Select(item => new
                {
                    productID = item.ProductID,
                    quantity = item.Quantity,
                    price = _productRepository.GetProductPrice(item.ProductID),
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating order summary: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "An error occurred while updating the order summary.");
        }
    }
    [HttpGet("{id}/cart")]
    public async Task<ActionResult<IEnumerable<OrderItem>>> GetCartItems(int id)
    {
        try
        {
            var order = await _orderRepository.GetOrderByIdWithItemsAsync(id);
            if (order == null)
            {
                return NotFound(new { message = "Order not found." });
            }

            return Ok(new
            {
                message = "Cart items fetched successfully!",
                cartItems = order.OrderItems.Select(item => new
                {
                    productID = item.ProductID,
                    quantity = item.Quantity
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching cart items: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "An error occurred while fetching cart items.");
        }
    }

    [HttpGet("{id}/summary")]
    public async Task<ActionResult> GetOrderSummary(int id)
    {
        try
        {
            var order = await _orderRepository.GetOrderByIdWithItemsAsync(id);
            if (order == null)
            {
                return NotFound(new { message = "Order not found." });
            }

            _orderRepository.CalculateOrderAmounts(order);

            var orderItems = order.OrderItems.Select(item => new
            {
                productID = item.ProductID,
                quantity = item.Quantity,
                unitPrice = item.UnitPrice,
                lineTotal = item.UnitPrice * item.Quantity
            }).ToList();

            return Ok(new
            {
                message = "Order summary fetched successfully!",
                orderID = order.OrderID,
                orderStatus = order.OrderStatus,
                createdAt = order.CreatedAt, // ✅ Add this

                orderItems = orderItems,
                subtotal = order.Subtotal,
                discountAmount = order.DiscountAmount,
                appliedOffer = order.AppliedOffer != null
                    ? new
                    {
                        offerID = order.AppliedOffer.OfferID,
                        description = order.AppliedOffer.Description,
                        discountType = order.AppliedOffer.DiscountAmount.HasValue ? "Flat" : "Percent",
                        discountValue = order.AppliedOffer.DiscountAmount ?? (decimal?)order.AppliedOffer.DiscountPercent
                    }
                    : null,
                cgst = order.CGST,
                sgst = order.SGST,
                serviceCharge = order.ServiceCharge,
                totalAmount = order.TotalAmount
            });

        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching order summary: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "An error occurred while fetching the order summary.");
        }
    }


    [HttpGet("with-waiter/{waiterUserId}")]
    public IActionResult GetOrdersByWaiter(int waiterUserId)
    {
        var orders = _context.Orders
            .Where(o => o.WaiterUserID == waiterUserId)
            .ToList();

        return Ok(orders);
    }


    [HttpPost("{orderId}/confirm")]
    public async Task<IActionResult> ConfirmOrder(int orderId)
    {
        try
        {
            var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning($"Order not found. OrderID: {orderId}");
                return NotFound(new { message = "Order not found.", orderId });
            }

            // Validate order can be confirmed
            if (order.OrderStatus != OrderStatus.Pending)
            {
                return BadRequest(new
                {
                    message = $"Order is already {order.OrderStatus}",
                    currentStatus = order.OrderStatus.ToString()
                });
            }

            // Update status
            order.OrderStatus = OrderStatus.Confirmed;
            order.KitchenStatus = KitchenStatus.Pending;
            order.UpdatedAt = DateTime.UtcNow;

            // Save changes
            var updatedOrder = await _orderRepository.UpdateOrderAsync(order);
            if (updatedOrder == null)
            {
                _logger.LogError($"Failed to update OrderID: {orderId}");
                return StatusCode(500, new { message = "Failed to update the order.", orderId });
            }

            _logger.LogInformation($"OrderID: {orderId} confirmed successfully. New status: {updatedOrder.OrderStatus}");

            return Ok(new
            {
                message = "Order confirmed successfully!",
                orderID = order.OrderID,
                orderStatus = order.OrderStatus,
                kitchenStatus = order.KitchenStatus,
                updatedAt = order.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error confirming OrderID: {orderId}. Exception: {ex.Message}");
            return StatusCode(500, new
            {
                message = "An error occurred while confirming the order.",
                error = ex.Message
            });
        }
    }

    [HttpGet("kitchen/pending-orders")]
    public async Task<IActionResult> GetPendingKitchenOrders()
    {
        var allUnpreparedItems = await _context.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.Product)
            .Include(oi => oi.Customizations)
                .ThenInclude(c => c.CustomizationOption)
         .Where(oi =>
    oi.Order.OrderStatus == OrderStatus.Confirmed &&
    oi.Product != null &&
    oi.Order != null)

            .ToListAsync();

        var grouped = allUnpreparedItems
            .GroupBy(oi => new { oi.OrderID, oi.BatchID })
            .Select(group =>
            {
                var firstItem = group.First();
                var order = firstItem.Order;

                // ✅ New per-batch kitchen status based only on these items
                var batchKitchenStatus = group.All(x => x.IsPrepared) ? KitchenStatus.Ready :
                                         group.Any(x => x.IsPrepared) ? KitchenStatus.Preparing :
                                         KitchenStatus.Pending;

                return new
                {
                    orderID = group.Key.OrderID,
                    batchID = group.Key.BatchID,
                    restaurantTableID = order.RestaurantTableID,
                    createdAt = group.Max(x => x.AddedToKitchenAt),
                    playSound = group.Any(x => !x.IsPrepared), // If any in batch is unprepared
                    lastKitchenReadyAt = order.LastKitchenReadyAt,
                    kitchenStatus = batchKitchenStatus, // ✅ Real status per batch
                    items = group.Select(oi => new
                    {
                        productID = oi.ProductID,
                        name = oi.Product.ProductName,
                        quantity = oi.Quantity,
                        isPrepared = oi.IsPrepared,
                        customizations = oi.Customizations.Select(c => new
                        {
                            customizationOptionID = c.CustomizationOptionID,
                            optionName = c.CustomizationOption.Name
                        }),
                        addedToKitchenAt = oi.AddedToKitchenAt
                    }).ToList()
                };
            })
            .Where(group =>
                group.items.Any(i => !i.isPrepared)) // ✅ Show only batches with pending items
            .OrderBy(g => g.createdAt)
            .ToList();

        return Ok(new
        {
            message = "Pending kitchen orders fetched successfully",
            orders = grouped
        });
    }
    [HttpGet("kitchen/history-orders")]
    public async Task<IActionResult> GetKitchenHistoryOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product) // Include Product
            .Where(o => o.KitchenStatus == KitchenStatus.Ready)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var result = orders.Select(o => new {
            o.OrderID,
            o.RestaurantTableID,
            o.KitchenStatus,
            CreatedAt = o.CreatedAt,
            Items = o.OrderItems.Select(oi => new {
                ProductID = oi.ProductID,
                Name = oi.Product.ProductName, // Add product name
                oi.Quantity,
                Customizations = oi.Customizations.Select(c => new {
                    c.CustomizationOptionID,
                    OptionName = c.CustomizationOption.Name
                }).ToList()
            }).ToList()
        });

        return Ok(new { orders = result });
    }


    [HttpPut("kitchen/update-batch-status/{orderId}")]
    public async Task<IActionResult> UpdateBatchStatus(int orderId, [FromBody] JsonElement payload)
    {
        try
        {
            if (!payload.TryGetProperty("status", out var statusProp) ||
                !payload.TryGetProperty("batchID", out var batchProp))
                return BadRequest("Missing status or batchID.");

            string status = statusProp.GetString()?.Trim().ToLower(); // normalized
            int batchId = batchProp.GetInt32();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
                return NotFound("Order not found.");

            var itemsInBatch = order.OrderItems.Where(oi => oi.BatchID == batchId).ToList();
            if (!itemsInBatch.Any())
                return NotFound("No items found in this batch.");

            if (status == "preparing")
            {
                foreach (var batchItem in itemsInBatch)
                {
                    batchItem.IsPrepared = false;
                }

                order.KitchenStatus = KitchenStatus.Preparing;
            }
            else if (status == "ready")
            {
                foreach (var batchItem in itemsInBatch)
                {
                    batchItem.IsPrepared = true;
                    batchItem.PreparedAt = DateTime.UtcNow;
                }

                order.KitchenStatus = KitchenStatus.Ready;
                order.LastKitchenReadyAt = DateTime.UtcNow;

                var notification = new WaiterNotification
                {
                    OrderId = orderId,
                    TableNo = order.RestaurantTableID,
                    Message = $"Order #{orderId} for Table {order.RestaurantTableID} is ready",
                    CreatedAt = DateTime.UtcNow,
                    IsAcknowledged = false
                };
                _context.WaiterNotifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Batch {batchId} updated to {status}." });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating batch status: {ex.Message}");
            return StatusCode(500, "Failed to update batch status.");
        }
    }


    [HttpPost("{orderId}/mark-ready")]
    public async Task<IActionResult> MarkOrderReady(int orderId)
    {
        var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId);
        if (order == null)
            return NotFound(new { message = "Order not found." });

        var now = DateTime.UtcNow;

        // Get the latest batch ID
        var latestBatchId = order.OrderItems.Max(oi => oi.BatchID);

        // Mark all items in latest batch as prepared
        foreach (var item in order.OrderItems.Where(oi => oi.BatchID == latestBatchId))
        {
            item.IsPrepared = true;
            item.PreparedAt = now;
        }

        order.LastKitchenReadyAt = now;
        order.KitchenStatus = KitchenStatus.Ready;
        order.OrderStatus = OrderStatus.Confirmed;
        order.UpdatedAt = now;

        // ✅ Add waiter notification logic here
        var notification = new WaiterNotification
        {
            OrderId = orderId,
            TableNo = order.RestaurantTableID,
            Message = $"Order #{orderId} for Table {order.RestaurantTableID} is ready",
            CreatedAt = now,
            IsAcknowledged = false
        };
        _context.WaiterNotifications.Add(notification);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Order marked as ready!", orderID = orderId });
    }



    [HttpPut("kitchen/mark-sound-played/{orderId}")]
    public async Task<IActionResult> MarkSoundPlayed(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return NotFound();

        order.PlaySound = false;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"PlaySound cleared for Order {orderId}" });
    }

    // In OrderController.cs
    [HttpPut("kitchen/update-status/{orderId}")]
    public async Task<IActionResult> UpdateKitchenStatus(int orderId, [FromBody] string status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return NotFound();

        if (status.Equals("Ready", StringComparison.OrdinalIgnoreCase))
        {
            order.KitchenStatus = KitchenStatus.Ready;
            order.OrderStatus = OrderStatus.Confirmed;

            // Create notification for waiter
            var notification = new WaiterNotification
            {
                OrderId = orderId,
                TableNo = order.RestaurantTableID,
                Message = $"Order #{orderId} for Table {order.RestaurantTableID} is ready",
                CreatedAt = DateTime.UtcNow,
                IsAcknowledged = false
            };
            _context.WaiterNotifications.Add(notification);
        }
        else
        {
            order.KitchenStatus = KitchenStatus.Preparing;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Kitchen status updated!" });
    }

    [HttpGet("waiter/notifications")]
    public async Task<IActionResult> GetWaiterNotifications()
    {
        var notifications = await _context.WaiterNotifications
            .Where(n => !n.IsAcknowledged)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpPut("waiter/notifications/{notificationId}/acknowledge")]
    public async Task<IActionResult> AcknowledgeNotification(int notificationId)
    {
        var notification = await _context.WaiterNotifications.FindAsync(notificationId);
        if (notification == null) return NotFound();

        notification.IsAcknowledged = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }





    [HttpPut("{orderId}/serve")]
    public async Task<IActionResult> ServeOrder(int orderId)
    {
        try
        {
            var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId);
            if (order == null)
            {
                return NotFound(new { message = "Order not found." });
            }

            if (order.KitchenStatus != KitchenStatus.Ready)
            {
                return BadRequest(new { message = "Order is not ready to serve yet." });
            }

            // Only update OrderStatus, not KitchenStatus
            order.OrderStatus = OrderStatus.Served;
            order.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.UpdateOrderAsync(order);

            return Ok(new
            {
                message = "Order served successfully!",
                orderID = order.OrderID,
                orderStatus = order.OrderStatus
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error serving order: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "An error occurred while serving the order.");
        }
    }

    [HttpPut("{orderId}/complete")]
    public async Task<IActionResult> CompleteOrder(int orderId)
    {
        try
        {
            var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId);
            if (order == null)
            {
                return NotFound(new { message = "Order not found." });
            }
            order.ClosedAt = DateTime.UtcNow;
            order.OrderStatus = OrderStatus.Completed;

            await _orderRepository.UpdateOrderAsync(order);

            return Ok(new
            {
                message = "Order completed successfully!",
                orderID = order.OrderID,
                orderStatus = order.OrderStatus
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error completing order: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "An error occurred while completing the order.");
        }
    }
    [HttpPut("{orderId}/cancel")]
    public async Task<IActionResult> CancelOrder(int orderId)
    {
        try
        {
            var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId);
            if (order == null)
            {
                return NotFound(new { message = "Order not found." });
            }
            order.ClosedAt = DateTime.UtcNow;
            order.OrderStatus = OrderStatus.Cancelled;

            await _orderRepository.UpdateOrderAsync(order);

            return Ok(new
            {
                message = "Order cancelled successfully!",
                orderID = order.OrderID,
                orderStatus = order.OrderStatus
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error cancelling order: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "An error occurred while cancelling the order.");
        }
    }

    [HttpPut("waiter-requests/{id}/accept")]
    public async Task<IActionResult> AcceptWaiterRequest(int id)
    {
        var request = await _context.WaiterRequests.FindAsync(id);
        if (request == null)
            return NotFound(new { message = "Request not found." });

        _context.WaiterRequests.Remove(request);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Request accepted and removed from list." });
    }

    [HttpPut("kitchen/update-order-status/{orderId}")]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] string status)
    {
        if (!Enum.TryParse(status, true, out OrderStatus orderStatus))
        {
            return BadRequest(new { message = "Invalid order status." });
        }

        var updated = await _orderRepository.UpdateOrderStatusAsync(orderId, orderStatus);
        if (!updated) return NotFound(new { message = "Order not found." });

        return Ok(new { message = $"Order {orderId} status updated to {orderStatus}." });
    }


    [HttpPut("waiter/update-order-status/{orderId}")]
    public async Task<IActionResult> UpdateOrderStatusByWaiter(int orderId, [FromBody] string status)
    {
        if (!Enum.TryParse(status, true, out OrderStatus orderStatus))
        {
            return BadRequest(new { message = "Invalid order status." });
        }

        if (orderStatus != OrderStatus.Confirmed && orderStatus != OrderStatus.Cancelled)
        {
            return BadRequest(new { message = "Waiters can only update orders to Confirmed or Cancelled." });
        }

        var updated = await _orderRepository.UpdateOrderStatusAsync(orderId, orderStatus);
        if (!updated) return NotFound(new { message = "Order not found." });

        return Ok(new { message = $"Order {orderId} status updated to {orderStatus} by Waiter." });
    }

    [HttpPut("{orderId}/assign-waiter/{waiterId}")]
    public async Task<IActionResult> AssignWaiterToOrder(int orderId, int waiterId)
    {
        try
        {
            var success = await _orderRepository.AssignWaiterToOrderAsync(orderId, waiterId);
            if (!success) return NotFound(new { message = "Order or Waiter not found." });

            return Ok(new { message = $"Waiter {waiterId} assigned to Order {orderId} successfully!" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error assigning waiter: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "An error occurred while assigning the waiter.");
        }
    }


    [HttpGet("waiter-requests")]
    public IActionResult GetWaiterRequests()
    {
        var requests = _context.WaiterRequests
            .OrderByDescending(r => r.RequestTime)
            .ToList();

        return Ok(new { data = requests });
    }

    [HttpPost("uploadImage")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage([FromForm] UploadImageRequest request)
    {
        var file = request.File;

        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return Ok(new { imagePath = $"/uploads/{uniqueFileName}" });
    }

    [HttpPost("rate-order")]
    public async Task<IActionResult> RateOrder([FromBody] Review review)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderID == review.OrderID);
        if (order == null)
            return NotFound(new { message = $"Order {review.OrderID} not found." });

        var alreadyReviewed = await _context.Reviews
            .AnyAsync(r => r.OrderID == review.OrderID);
        if (alreadyReviewed)
            return BadRequest(new { message = "You have already reviewed this order." });

        review.CreatedAt = DateTime.UtcNow;
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Thank you for rating your order!" });
    }


    [HttpGet("{orderId}/bill")]
    public async Task<IActionResult> DownloadBill(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.RestaurantTable)
            .FirstOrDefaultAsync(o => o.OrderID == orderId);

        if (order == null)
            return NotFound();

        var restaurant = await _context.Restaurants.FirstOrDefaultAsync();

        // ✅ Ensure totals & offer are applied
        _orderRepository.CalculateOrderAmounts(order);
        await _orderRepository.ApplyBestAvailableOfferAsync(order);
        await _context.SaveChangesAsync();

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);

                // ✅ HEADER
                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(restaurant?.Name ?? "Restaurant Name")
                        .Bold().FontSize(22);

                    if (!string.IsNullOrEmpty(restaurant?.Description))
                        col.Item().AlignCenter().Text(restaurant.Description).FontSize(12).Italic();

                    var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                    col.Item().AlignCenter().Text($"Date: {istTime:dd MMM yyyy | hh:mm tt}")
                        .FontSize(10).FontColor(Colors.Grey.Darken2);

                    col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                // ✅ ORDER DETAILS
                page.Content().Column(column =>
                {
                    column.Item().PaddingBottom(10).Text($"Order ID: #{order.OrderID}")
                        .Bold().FontSize(14);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(10, QuestPDF.Infrastructure.Unit.Millimetre);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("#").Bold();
                            header.Cell().Text("Item").Bold();
                            header.Cell().AlignRight().Text("Qty").Bold();
                            header.Cell().AlignRight().Text("Price").Bold();
                            header.Cell().AlignRight().Text("Total").Bold();
                        });

                        foreach (var (item, index) in order.OrderItems.Select((x, i) => (x, i)))
                        {
                            table.Cell().Text($"{index + 1}");
                            table.Cell().Text(item.Product?.ProductName ?? "Unknown");
                            table.Cell().AlignRight().Text(item.Quantity.ToString());
                            table.Cell().AlignRight().Text($"₹{item.UnitPrice:N2}");
                            table.Cell().AlignRight().Text($"₹{item.UnitPrice * item.Quantity:N2}");
                        }
                    });

                    // ✅ TOTALS SECTION
                    column.Item().PaddingTop(15).AlignRight().Text(text =>
                    {
                        text.Span("Subtotal: ").Bold();
                        text.Span($"₹{order.Subtotal:N2}");
                        text.EmptyLine();

                        // ✅ DISCOUNT SECTION
                        if (order.AppliedOffer != null)
                        {
                            text.Span("Discount (");
                            text.Span(order.AppliedOffer.Description ?? "Offer").Italic();
                            text.Span("): ").Bold();
                            text.Span($"- ₹{order.DiscountAmount:N2}");
                            text.EmptyLine();
                        }

                        text.Span("CGST: ").Bold();
                        text.Span($"₹{order.CGST:N2}");
                        text.EmptyLine();

                        text.Span("SGST: ").Bold();
                        text.Span($"₹{order.SGST:N2}");
                        text.EmptyLine();

                        text.Span("Service Charge: ").Bold();
                        text.Span($"₹{order.ServiceCharge:N2}");
                        text.EmptyLine();

                        text.Span("Total: ").Bold().FontSize(14);
                        text.Span($"₹{order.TotalAmount:N2}").FontSize(14);
                    });
                });

                // ✅ FOOTER
                page.Footer().Column(col =>
                {
                    col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    col.Item().AlignCenter().Text("Thank you for dining with us!").Bold().FontSize(12);
                    col.Item().AlignCenter().Text("Visit us again.").FontSize(10).Italic();
                });
            });
        }).GeneratePdf();

        return File(pdfBytes, "application/pdf", $"Bill_Order_{orderId}.pdf");
    }




    [HttpPost("call-waiter")]
    public async Task<IActionResult> CallWaiter([FromBody] WaiterRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Message))
            return BadRequest("Invalid waiter request");

        request.RequestTime = DateTime.UtcNow;
        request.IsNotified = false; // Initially not notified

        _context.WaiterRequests.Add(request);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Waiter request sent successfully",
            data = request
        });
    }

    [HttpGet("waiter/requests/unnotified")]
    public async Task<IActionResult> GetUnnotifiedWaiterRequests()
    {
        var requests = await _context.WaiterRequests
            .Where(r => !r.IsNotified)
            .OrderByDescending(r => r.RequestTime)
            .ToListAsync();

        return Ok(requests);
    }

    [HttpPut("waiter/requests/mark-notified/{id}")]
    public async Task<IActionResult> MarkRequestAsNotified(int id)
    {
        var request = await _context.WaiterRequests.FindAsync(id);
        if (request == null) return NotFound();

        request.IsNotified = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("waiter-requests/{id}")]
    public async Task<IActionResult> DeleteWaiterRequest(int id)
    {
        var request = await _context.WaiterRequests.FindAsync(id);
        if (request == null) return NotFound();

        _context.WaiterRequests.Remove(request);
        await _context.SaveChangesAsync();

        return NoContent();
    }


    [HttpPut("order/{orderId}/mark-served")]
    public IActionResult MarkOrderAsServed(int orderId)
    {
        // Mark order served in DB here
        return Ok(new { message = "Order marked as served" });
    }


    [HttpPost("{orderId}/pending")]
    public async Task<IActionResult> CreatePendingPayment(int orderId, [FromBody] dynamic payload)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.OrderID == orderId);
        if (order == null) return NotFound();

        // ✅ Calculate total amount before saving payment
        _orderRepository.CalculateOrderAmounts(order);
        await _context.SaveChangesAsync();

        string method = "Cash";
        if (payload is JsonElement jsonElement && jsonElement.TryGetProperty("method", out var methodProp))
        {
            method = methodProp.GetString() ?? "Cash";
        }

        _orderRepository.CalculateOrderAmounts(order);
        await _context.SaveChangesAsync();

        var payment = new Payment
        {
            OrderID = orderId,
            TableNo = order.RestaurantTableID,
            PaymentMethod = method,
            PaymentStatus = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            IsNotified = false,
            Amount = order.TotalAmount
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Pending payment created", paymentId = payment.PaymentID });
    }


    [HttpGet("pending-payments/unnotified")]
    public async Task<IActionResult> GetUnnotifiedPayments()
    {
        var payments = await _context.Payments
            .Where(p => !p.IsNotified && p.PaymentStatus == PaymentStatus.Pending)
            .ToListAsync();

        return Ok(payments);
    }


    [HttpGet("{restaurantId}/payment-details")]
    public async Task<IActionResult> GetRestaurantPaymentDetails(int restaurantId)
    {
        var restaurant = await _context.Restaurants.FindAsync(restaurantId);
        if (restaurant == null)
            return NotFound(new { message = "Restaurant not found" });

        return Ok(new
        {
            upiID = restaurant.UPI_ID,  // ✅ Fixed property name
            upiName = restaurant.UPI_Name ?? restaurant.Name  // ✅ Fixed property name
        });
    }

    [HttpGet("table/{tableId}/payment-details")]
    public async Task<IActionResult> GetRestaurantPaymentDetailsByTable(int tableId)
    {
        var table = await _context.RestaurantTables
            .Include(t => t.Restaurant)
            .FirstOrDefaultAsync(t => t.RestaurantTableID == tableId);

        if (table == null || table.Restaurant == null)
            return NotFound(new { message = "Table or Restaurant not found" });

        var restaurant = table.Restaurant;

        return Ok(new
        {
            restaurantID = restaurant.RestaurantID,
            name = restaurant.Name,
            description = restaurant.Description,
            logoPath = restaurant.LogoPath,
            upiID = restaurant.UPI_ID,
            upiName = restaurant.UPI_Name ?? restaurant.Name
        });
    }




    [HttpPut("pending-payments/{id}/clear")]
    public async Task<IActionResult> ClearPendingPayment(int id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null)
            return NotFound(new { message = "Payment not found." });

        // Mark payment as completed
        payment.PaymentStatus = PaymentStatus.Success;
        payment.CompletedAt = DateTime.UtcNow;

        // ✅ Close order as well
        var order = await _context.Orders.FindAsync(payment.OrderID);
        if (order != null)
        {
            order.OrderStatus = OrderStatus.Completed;
            order.ClosedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "✅ Payment completed and order closed successfully." });
    }

    [HttpPost("{orderId}/initiate-payment")]
    public async Task<IActionResult> InitiatePayment(int orderId, [FromQuery] string method = "UPI")
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);
            if (order == null)
                return NotFound(new { message = "Order not found." });

            _orderRepository.CalculateOrderAmounts(order);
            await _context.SaveChangesAsync();

            var transactionId = $"DIGIEAT_{orderId}_{DateTime.Now:yyyyMMddHHmmss}";

            var payment = new Payment
            {
                OrderID = orderId,
                TableNo = order.RestaurantTableID,
                Amount = order.TotalAmount,
                PaymentMethod = method,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            if (method.Equals("UPI", StringComparison.OrdinalIgnoreCase))
            {
                var restaurant = await _context.RestaurantTables
                    .Where(rt => rt.RestaurantTableID == order.RestaurantTableID)
                    .Select(rt => rt.Restaurant)
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    method = "UPI",
                    upiId = restaurant?.UPI_ID,
                    upiName = restaurant?.UPI_Name ?? restaurant?.Name,
                    amount = order.TotalAmount,
                    transactionId,
                    orderId
                });
            }

            // ✅ Return only basic details for non-UPI methods like Cash/Card
            return Ok(new
            {
                method = method,
                message = "Payment initiated successfully!",
                paymentId = payment.PaymentID,
                orderId = payment.OrderID,
                amount = payment.Amount,
                status = payment.PaymentStatus.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error initiating payment: {ex.Message}");
            return StatusCode(500, new { message = "Error initiating payment.", error = ex.Message });
        }
    }



    [HttpGet("{orderId}/payment-status")]
    public async Task<IActionResult> GetPaymentStatus(int orderId)
    {
        try
        {
            var payment = await _context.Payments
                .Where(p => p.OrderID == orderId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (payment == null)
            {
                return NotFound(new { message = "Payment not found." });
            }

            return Ok(new
            {
                paymentId = payment.PaymentID,
                orderId = payment.OrderID,
                method = payment.PaymentMethod,
                status = payment.PaymentStatus.ToString(),
                amount = payment.Amount,
                createdAt = payment.CreatedAt,
                completedAt = payment.CompletedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error checking payment status: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "An error occurred while checking payment status.");
        }
    }

    [HttpPut("payments/{paymentId}/complete")]
    [Authorize(Roles = "waiter,admin")]
    public async Task<IActionResult> CompletePayment(int paymentId)
    {
        try
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null)
            {
                return NotFound(new { message = "Payment not found." });
            }

            payment.PaymentStatus = PaymentStatus.Success;
            payment.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Payment completed successfully!",
                paymentId = payment.PaymentID,
                orderId = payment.OrderID,
                status = payment.PaymentStatus.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error completing payment: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "An error occurred while completing payment.");
        }
    }

    [HttpGet("pending-payments")]
    public async Task<IActionResult> GetPendingPayments()
    {
        try
        {
            var payments = await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .Where(p => p.PaymentStatus == PaymentStatus.Pending)  // ❌ Removed IsNotified condition
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(payments.Select(p => new
            {
                paymentId = p.PaymentID,
                orderId = p.OrderID,
                tableNo = p.TableNo,
                method = p.PaymentMethod,
                amount = p.Amount,
                createdAt = p.CreatedAt,
                isNotified = p.IsNotified,
                items = p.Order?.OrderItems.Select(oi => new
                {
                    productName = oi.Product?.ProductName ?? "Unknown",
                    quantity = oi.Quantity
                }).ToList()
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching pending payments: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "An error occurred while fetching pending payments.");
        }
    }




    [HttpPut("payments/{paymentId}/mark-notified")]
    [Authorize(Roles = "waiter,admin")]
    public async Task<IActionResult> MarkPaymentNotified(int paymentId)
    {
        try
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null)
            {
                return NotFound(new { message = "Payment not found." });
            }

            payment.IsNotified = true;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Payment marked as notified!",
                paymentId = payment.PaymentID,
                isNotified = payment.IsNotified
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error marking payment as notified: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "An error occurred while marking payment as notified.");
        }
    }

    [HttpGet("waiter-requests/unnotified")]
    public async Task<IActionResult> GetUnnotifiedPendingPayments()
    {
        var payments = await _context.Payments
            .Where(p => !p.IsNotified && p.PaymentStatus == PaymentStatus.Pending)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        return Ok(payments);
    }

    [HttpPut("pending-payments/{id}/mark-notified")]
    public async Task<IActionResult> MarkPaymentAsNotified(int id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null) return NotFound();

        payment.IsNotified = true;
        await _context.SaveChangesAsync();

        return Ok();
    }

    //// Fetch all franchises (restaurants)
    //[HttpGet("franchises")]
    //public async Task<IActionResult> GetAllFranchises()
    //{
    //    var franchises = await _context.Restaurants
    //        .Select(r => new
    //        {
    //            restaurantId = r.RestaurantID,
    //            name = r.Name,
    //            upiId = r.UPI_ID
    //        })
    //        .ToListAsync();

    //    return Ok(franchises);
    //}

    // Filter orders by franchise (restaurant)
    [HttpGet("with-waiter/by-franchise/{restaurantId}")]
    public async Task<IActionResult> GetOrdersByFranchise(int restaurantId)
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Payments.OrderByDescending(p => p.CreatedAt))
            .Where(o => o.RestaurantTable.RestaurantID == restaurantId)  // 👈 Key Filtering
            .ToListAsync();

        return Ok(new
        {
            message = "Orders fetched successfully for franchise.",
            orders = orders.Select(order => new
            {
                orderID = order.OrderID,
                createdAt = order.CreatedAt,
                closedAt = order.ClosedAt,
                tableNo = order.RestaurantTableID,
                restaurantId = order.RestaurantTable.RestaurantID,  // ✅ Add this

                orderStatus = Enum.GetName(typeof(OrderStatus), order.OrderStatus),
                items = order.OrderItems.Select(item => new
                {
                    productID = item.ProductID,
                    productName = item.Product?.ProductName ?? $"Product {item.ProductID}",
                    quantity = item.Quantity
                }),
                latestPayment = order.Payments.FirstOrDefault() == null ? null : new
                {
                    method = order.Payments.FirstOrDefault().PaymentMethod,
                    status = order.Payments.FirstOrDefault().PaymentStatus.ToString(),
                    amount = order.Payments.FirstOrDefault().Amount,
                    paidAt = order.Payments.FirstOrDefault().CompletedAt
                }
            })
        });
    }




    // GET ALL FRANCHISES
    //[HttpGet("franchises")]
    //public async Task<IActionResult> GetFranchises()
    //{
    //    var franchises = await _context.Franchises.ToListAsync();
    //    return Ok(franchises);
    //}

    //// ADD NEW FRANCHISE
    //[HttpPost("franchises")]
    //public async Task<IActionResult> AddFranchise([FromBody] Franchise franchise)
    //{
    //    _context.Franchises.Add(franchise);
    //    await _context.SaveChangesAsync();
    //    return Ok(new { message = "Franchise added successfully." });
    //}

    //// UPDATE FRANCHISE
    //[HttpPut("franchises/{franchiseId}")]
    //public async Task<IActionResult> UpdateFranchise(int franchiseId, [FromBody] Franchise franchise)
    //{
    //    var existing = await _context.Franchises.FindAsync(franchiseId);
    //    if (existing == null) return NotFound();

    //    existing.FranchiseName = franchise.FranchiseName;
    //    existing.ManagerName = franchise.ManagerName;
    //    existing.ContactNumber = franchise.ContactNumber;
    //    existing.Address = franchise.Address;

    //    await _context.SaveChangesAsync();
    //    return Ok(new { message = "Franchise updated successfully." });
    //}

    //// DELETE FRANCHISE
    //[HttpDelete("franchises/{franchiseId}")]
    //public async Task<IActionResult> DeleteFranchise(int franchiseId)
    //{
    //    var franchise = await _context.Franchises.FindAsync(franchiseId);
    //    if (franchise == null) return NotFound();

    //    _context.Franchises.Remove(franchise);
    //    await _context.SaveChangesAsync();
    //    return Ok(new { message = "Franchise deleted successfully." });
    //}

    [HttpGet("status/{orderId}")]
    public async Task<IActionResult> GetOrderStatus(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);

        if (order == null)
            return NotFound();

        return Ok(new
        {
            orderID = order.OrderID,
            orderStatus = order.OrderStatus,
            createdAt = order.CreatedAt
        });
    }
    [HttpGet("report/sales-summary")]
    public async Task<IActionResult> GetSalesAnalyticsReport(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            endDate = endDate.HasValue ? EnsureUtc(endDate.Value) : DateTime.UtcNow;
            startDate = startDate.HasValue ? EnsureUtc(startDate.Value) : endDate.Value.AddDays(-30);

            var totalOrders = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .ToListAsync();

            var totalRevenue = totalOrders
                .SelectMany(o => o.Payments)
                .Sum(p => p.Amount);

            var orderCount = totalOrders.Count;
            var averageOrderValue = orderCount > 0 ? totalRevenue / orderCount : 0;

            return Ok(new
            {
                totalOrders = orderCount,
                totalRevenue,
                avgOrderValue = averageOrderValue
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sales analytics report.");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }


    [HttpGet("report/item-summary")]
    public async Task<IActionResult> GetItemSummaryReport(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            endDate = endDate.HasValue ? EnsureUtc(endDate.Value) : DateTime.UtcNow;
            startDate = startDate.HasValue ? EnsureUtc(startDate.Value) : endDate.Value.AddDays(-30);

            var itemSummary = await _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.CreatedAt >= startDate && oi.CreatedAt <= endDate)
                .GroupBy(oi => new { oi.ProductID, oi.Product.ProductName })
                .Select(g => new
                {
                    productID = g.Key.ProductID,
                    productName = g.Key.ProductName,
                    quantitySold = g.Sum(x => x.Quantity),
                    totalRevenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(x => x.quantitySold)
                .ToListAsync();

            return Ok(itemSummary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating item summary report.");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("report/category-revenue")]
    public async Task<IActionResult> GetCategoryRevenueReport(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            endDate = endDate.HasValue ? EnsureUtc(endDate.Value) : DateTime.UtcNow;
            startDate = startDate.HasValue ? EnsureUtc(startDate.Value) : endDate.Value.AddDays(-30);

            var categoryRevenue = await _context.OrderItems
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.SubCategory)
                        .ThenInclude(sc => sc.Category)
                .Where(oi => oi.CreatedAt >= startDate && oi.CreatedAt <= endDate)
                .GroupBy(oi => oi.Product.SubCategory.Category.CategoryName)
                .Select(g => new
                {
                    category = g.Key,
                    revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .ToListAsync();

            return Ok(categoryRevenue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating category revenue report.");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }



    [HttpGet("report/category-summary")]
    public async Task<IActionResult> GetCategoryRevenue()
    {
        var items = await _context.OrderItems
            .Include(oi => oi.Product)
                .ThenInclude(p => p.Category)
            .Include(oi => oi.Order)
            .Where(oi => oi.Order.OrderStatus != OrderStatus.Cancelled)
            .ToListAsync();

        var grouped = items
            .GroupBy(i => i.Product.Category.CategoryName)
            .Select(g => new
            {
                Category = g.Key,
                Revenue = g.Sum(x => x.Quantity * x.Product.Price)
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        return Ok(grouped);
    }

    private DateTime EnsureUtc(DateTime dateTime)
    {
        return dateTime.Kind == DateTimeKind.Utc
            ? dateTime
            : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }


    [HttpGet("report/order-summary")]
    public async Task<IActionResult> GetOrderSummaryReport(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            endDate = endDate.HasValue ? EnsureUtc(endDate.Value) : DateTime.UtcNow;
            startDate = startDate.HasValue ? EnsureUtc(startDate.Value) : endDate.Value.AddDays(-30);

            var orders = await _context.Orders
                .Include(o => o.Payments)
                .Where(o => o.OrderStatus != OrderStatus.Cancelled &&
                            o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .ToListAsync();

            var summary = orders
                .GroupBy(o => o.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    OrderCount = g.Count(),
                    TotalRevenue = g.Sum(o => o.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault()?.Amount ?? 0),
                    AverageBill = g.Average(o => o.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault()?.Amount ?? 0)
                })
                .ToList();

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating order summary report.");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }


    [HttpGet("report/monthly-summary")]
    public async Task<IActionResult> GetMonthlySummary()
    {
        var orders = await _context.Orders
            .Include(o => o.Payments)
            .Where(o => o.OrderStatus != OrderStatus.Cancelled && o.Payments.Any())
            .ToListAsync();

        var summary = orders
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                TotalOrders = g.Count(),
                TotalRevenue = g.Sum(o => o.Payments.FirstOrDefault()?.Amount ?? 0),
                AvgBill = g.Average(o => o.Payments.FirstOrDefault()?.Amount ?? 0)
            })
            .ToList();

        return Ok(summary);
    }
    [HttpGet("report/merged-bills")]
    public async Task<IActionResult> GetMergedBills([FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        start = start.HasValue ? EnsureUtc(start.Value) : null;
        end = end.HasValue ? EnsureUtc(end.Value) : null;

        var orders = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.OrderStatus != OrderStatus.Cancelled &&
                        (!start.HasValue || o.CreatedAt >= start.Value) &&
                        (!end.HasValue || o.CreatedAt <= end.Value))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("OrderID,Date,Table No,Product,Qty,Unit Price,Total");

        foreach (var order in orders)
        {
            foreach (var item in order.OrderItems)
            {
                var lineTotal = item.Quantity * item.UnitPrice;
                sb.AppendLine($"{order.OrderID},{order.CreatedAt:yyyy-MM-dd HH:mm},{order.RestaurantTableID},{item.Product?.ProductName},{item.Quantity},{item.UnitPrice},{lineTotal}");
            }
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", "merged-bills.csv");
    }




    [HttpGet("report/order-history")]
    public async Task<IActionResult> GetOrderHistoryReport(
      [FromQuery] DateTime? startDate,
      [FromQuery] DateTime? endDate,
      [FromQuery] string? status = null,
      [FromQuery] string? paymentMethod = null,
      [FromQuery] int? tableNo = null,
      [FromQuery] string? search = null)
    {
        try
        {
            if (!startDate.HasValue || !endDate.HasValue)
            {
                endDate = EnsureUtc(DateTime.Today);
                startDate = EnsureUtc(endDate.Value.AddDays(-30));
            }
            else
            {
                startDate = EnsureUtc(startDate.Value);
                endDate = EnsureUtc(endDate.Value.Date.AddDays(1).AddSeconds(-1));
            }

            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse(status, out OrderStatus orderStatus))
            {
                query = query.Where(o => o.OrderStatus == orderStatus);
            }

            if (!string.IsNullOrEmpty(paymentMethod))
            {
                query = query.Where(o => o.Payments.Any(p => p.PaymentMethod == paymentMethod));
            }

            if (tableNo.HasValue)
            {
                query = query.Where(o => o.RestaurantTableID == tableNo);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o =>
                    o.OrderID.ToString().Contains(search) ||
                    o.OrderItems.Any(oi => oi.Product.ProductName.Contains(search)));
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var result = orders.Select(o => new
            {
                OrderID = o.OrderID,
                CreatedAt = o.CreatedAt,
                ClosedAt = o.ClosedAt,
                TableNo = o.RestaurantTableID,
                Status = o.OrderStatus.ToString(),
                PaymentMethod = o.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault()?.PaymentMethod ?? "Pending",
                PaymentStatus = o.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault()?.PaymentStatus.ToString() ?? "Pending",
                Items = o.OrderItems.Select(oi => new
                {
                    ProductID = oi.ProductID,
                    ProductName = oi.Product.ProductName,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Customizations = oi.Customizations.Select(c => new
                    {
                        OptionID = c.CustomizationOptionID,
                        OptionName = c.CustomizationOption.Name
                    })
                }),
                Subtotal = o.Subtotal,
                DiscountAmount = o.DiscountAmount,
                CGST = o.CGST,
                SGST = o.SGST,
                ServiceCharge = o.ServiceCharge,
                TotalAmount = o.TotalAmount,
                Duration = o.ClosedAt.HasValue ? (o.ClosedAt.Value - o.CreatedAt).TotalMinutes : 0
            });

            return Ok(new
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalOrders = orders.Count,
                TotalRevenue = orders.Sum(o => o.TotalAmount),
                AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
                Orders = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating order history report: {ex.Message}");
            return StatusCode(500, "An error occurred while generating the report.");
        }
    }


    [HttpGet("report/sales-analytics")]
    public async Task<IActionResult> GetSalesAnalyticsReport(
      [FromQuery] DateTime? startDate,
      [FromQuery] DateTime? endDate,
      [FromQuery] string timeGrouping = "day", // day, week, month, hour
      [FromQuery] bool compareWithPrevious = false)
    {
        try
        {
            if (!startDate.HasValue || !endDate.HasValue)
            {
                endDate = EnsureUtc(DateTime.Today);
                startDate = EnsureUtc(endDate.Value.AddDays(-30));
            }
            else
            {
                endDate = EnsureUtc(endDate.Value.Date.AddDays(1).AddSeconds(-1));
                startDate = EnsureUtc(startDate.Value);
            }

            var mainData = await GetGroupedSalesData(startDate.Value, endDate.Value, timeGrouping);

            Dictionary<string, object> comparisonData = null;
            if (compareWithPrevious)
            {
                var duration = endDate.Value - startDate.Value;
                var comparisonStartDate = EnsureUtc(startDate.Value - duration);
                var comparisonEndDate = EnsureUtc(startDate.Value.AddSeconds(-1));
                comparisonData = await GetGroupedSalesData(comparisonStartDate, comparisonEndDate, timeGrouping);
            }

            var totalOrders = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .CountAsync();

            var totalRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .SumAsync(o => o.TotalAmount);

            var cancelledOrders = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate &&
                            o.OrderStatus == OrderStatus.Cancelled)
                .CountAsync();

            var paymentMethods = await _context.Payments
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new
                {
                    Method = g.Key,
                    Count = g.Count(),
                    Amount = g.Sum(p => p.Amount)
                })
                .ToListAsync();

            var topItems = await _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate)
                .GroupBy(oi => new { oi.ProductID, oi.Product.ProductName })
                .Select(g => new
                {
                    ProductID = g.Key.ProductID,
                    ProductName = g.Key.ProductName,
                    Quantity = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToListAsync();

            var categoryPerformance = await _context.OrderItems
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Category)
                .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate)
                .GroupBy(oi => new { oi.Product.Category.CategoryID, oi.Product.Category.CategoryName })
                .Select(g => new
                {
                    CategoryID = g.Key.CategoryID,
                    CategoryName = g.Key.CategoryName,
                    ItemCount = g.Count(),
                    Quantity = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            return Ok(new
            {
                StartDate = startDate,
                EndDate = endDate,
                TimeGrouping = timeGrouping,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                CancelledOrders = cancelledOrders,
                CancellationRate = totalOrders > 0 ? (decimal)cancelledOrders / totalOrders : 0,
                AverageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0,
                PaymentMethods = paymentMethods,
                TopItems = topItems,
                CategoryPerformance = categoryPerformance,
                TimeSeriesData = mainData,
                ComparisonData = comparisonData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating sales analytics report: {ex.Message}");
            return StatusCode(500, "An error occurred while generating the report.");
        }
    }


    private async Task<Dictionary<string, object>> GetGroupedSalesData(DateTime startDate, DateTime endDate, string timeGrouping)
    {
        startDate = EnsureUtc(startDate);
        endDate = EnsureUtc(endDate);

        var query = _context.Orders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate &&
                        o.OrderStatus != OrderStatus.Cancelled);

        IQueryable<dynamic> groupedQuery;

        switch (timeGrouping.ToLower())
        {
            case "hour":
                groupedQuery = query.GroupBy(o => new
                {
                    Date = o.CreatedAt.Date,
                    Hour = o.CreatedAt.Hour
                })
                .Select(g => new
                {
                    Key = $"{g.Key.Date:yyyy-MM-dd} {g.Key.Hour}:00",
                    g.Key.Date,
                    g.Key.Hour,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount),
                    AvgOrderValue = g.Average(o => o.TotalAmount)
                });
                break;

            case "week":
                groupedQuery = query
                    .AsEnumerable()
                    .GroupBy(o => new
                    {
                        WeekStart = o.CreatedAt.Date.AddDays(-(int)o.CreatedAt.DayOfWeek)
                    })
                    .Select(g => new
                    {
                        Key = $"Week of {g.Key.WeekStart:yyyy-MM-dd}",
                        OrderCount = g.Count(),
                        Revenue = g.Sum(o => o.TotalAmount),
                        AvgOrderValue = g.Average(o => o.TotalAmount)
                    })
                    .AsQueryable();
                break;

            case "month":
                groupedQuery = query.GroupBy(o => new
                {
                    o.CreatedAt.Year,
                    o.CreatedAt.Month
                })
                .Select(g => new
                {
                    Key = $"{new DateTime(g.Key.Year, g.Key.Month, 1):MMMM yyyy}",
                    g.Key.Year,
                    g.Key.Month,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount),
                    AvgOrderValue = g.Average(o => o.TotalAmount)
                });
                break;

            default: // day
                groupedQuery = query.GroupBy(o => o.CreatedAt.Date)
                    .Select(g => new
                    {
                        Key = $"{g.Key:yyyy-MM-dd}",
                        Date = g.Key,
                        OrderCount = g.Count(),
                        Revenue = g.Sum(o => o.TotalAmount),
                        AvgOrderValue = g.Average(o => o.TotalAmount)
                    });
                break;
        }

        var data = await groupedQuery.ToListAsync();

        return new Dictionary<string, object>
    {
        { "Labels", data.Select(x => x.Key).ToList() },
        { "OrderCounts", data.Select(x => x.OrderCount).ToList() },
        { "Revenues", data.Select(x => x.Revenue).ToList() },
        { "AvgOrderValues", data.Select(x => x.AvgOrderValue).ToList() }
    };
    }

    [HttpGet("/api/restauranttables")]
    public async Task<ActionResult<IEnumerable<object>>> GetRestaurantTables()
    {
        var tables = await _context.RestaurantTables
            .Include(t => t.Restaurant) // 👈 ensures Restaurant is loaded
            .Select(t => new
            {
                t.RestaurantTableID,
                t.TableName,
                t.Seats,
                t.RestaurantID,
                RestaurantName = t.Restaurant != null ? t.Restaurant.Name : null, // 👈 include name
                t.CreatedAt,
                t.UpdatedAt
            })
            .ToListAsync();

        return Ok(tables);
    }


    [HttpGet("{orderId}/bill-html")]
    public async Task<IActionResult> GetBillHtml(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.RestaurantTable)
            .FirstOrDefaultAsync(o => o.OrderID == orderId);

        if (order == null) return NotFound();

        var restaurant = await _context.Restaurants.FirstOrDefaultAsync();
        _orderRepository.CalculateOrderAmounts(order);
        await _orderRepository.ApplyBestAvailableOfferAsync(order);
        await _context.SaveChangesAsync();

        var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

        var html = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8'>
  <title>Order Bill #{order.OrderID}</title>
  <style>
    body {{
      font-family: 'Segoe UI', sans-serif;
      margin: 0;
      padding: 20px;
      background: #fff;
      color: #333;
    }}
    .bill-container {{
      max-width: 700px;
      margin: auto;
      border: 1px solid #ccc;
      padding: 20px;
      background: #fff;
    }}
    .restaurant-header {{
      text-align: center;
    }}
    .restaurant-header h2 {{
      margin-bottom: 4px;
    }}
    .restaurant-header p {{
      margin: 0;
      font-size: 14px;
      color: #777;
    }}
    .meta {{
      margin-top: 20px;
      font-size: 14px;
    }}
    .meta p {{
      margin: 5px 0;
    }}
    table {{
      width: 100%;
      border-collapse: collapse;
      margin-top: 20px;
      font-size: 14px;
    }}
    th, td {{
      border: 1px solid #ddd;
      padding: 8px;
      text-align: left;
    }}
    th {{
      background-color: #f8f8f8;
      font-weight: 600;
    }}
    tfoot td {{
      font-weight: bold;
    }}
    .totals {{
      margin-top: 20px;
      text-align: right;
    }}
    .totals p {{
      margin: 4px 0;
    }}
    .footer {{
      text-align: center;
      margin-top: 30px;
      font-size: 14px;
      color: #555;
    }}
  </style>
</head>
<body>
  <div class='bill-container'>
    <div class='restaurant-header'>
      <h2>{restaurant?.Name ?? "Restaurant Name"}</h2>
      <p>{restaurant?.Description ?? "Thanks for choosing us!"}</p>
    </div>

    <div class='meta'>
      <p><strong>Order ID:</strong> #{order.OrderID}</p>
      <p><strong>Date:</strong> {istNow:dd MMM yyyy hh:mm tt}</p>
      <p><strong>Table No:</strong> {order.RestaurantTable?.TableName ?? "N/A"}</p>
    </div>

    <table>
      <thead>
        <tr>
          <th>#</th>
          <th>Item</th>
          <th>Qty</th>
          <th>Rate</th>
          <th>Total</th>
        </tr>
      </thead>
      <tbody>";

        int count = 1;
        foreach (var item in order.OrderItems)
        {
            var total = item.Quantity * item.UnitPrice;
            html += $@"
        <tr>
          <td>{count++}</td>
          <td>{item.Product?.ProductName ?? "Unknown"}</td>
          <td>{item.Quantity}</td>
          <td>₹{item.UnitPrice:N2}</td>
          <td>₹{total:N2}</td>
        </tr>";
        }

        html += @"
      </tbody>
    </table>

    <div class='totals'>
      <p>Subtotal: ₹" + order.Subtotal.ToString("N2") + @"</p>";

        if (order.AppliedOffer != null)
        {
            html += "<p>Discount (" + order.AppliedOffer.Description + "): -₹" + order.DiscountAmount.ToString("N2") + "</p>";
        }

        html += $@"
      <p>CGST: ₹{order.CGST:N2}</p>
      <p>SGST: ₹{order.SGST:N2}</p>
      <p>Service Charge: ₹{order.ServiceCharge:N2}</p>
      <p><strong>Grand Total: ₹{order.TotalAmount:N2}</strong></p>
    </div>

    <div class='footer'>
      <p>Thank you for dining with us!</p>
      <p>Visit again 🙏</p>
    </div>
  </div>
</body>
</html>";

        return Content(html, "text/html");
    }



    [HttpGet("with-waiter")]
    public async Task<IActionResult> GetOrdersWithWaiters()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Payments.OrderByDescending(p => p.CreatedAt))
            .ToListAsync();

        // Calculate amounts for each order
        foreach (var order in orders)
        {
            _orderRepository.CalculateOrderAmounts(order);
        }

        return Ok(new
        {
            message = "Orders fetched successfully!",
            orders = orders.Select(order => new
            {
                orderID = order.OrderID,
                createdAt = order.CreatedAt,
                closedAt = order.ClosedAt,
                tableNo = order.RestaurantTableID,
                orderStatus = order.OrderStatus.ToString(),
                kitchenStatus = order.KitchenStatus.ToString(),
                subtotal = order.Subtotal,          // Add these
                discountAmount = order.DiscountAmount,
                cgst = order.CGST,
                sgst = order.SGST,
                serviceCharge = order.ServiceCharge,
                totalAmount = order.TotalAmount,
                items = order.OrderItems.Select(item => new
                {
                    productID = item.ProductID,
                    productName = item.Product?.ProductName ?? $"Product {item.ProductID}",
                    quantity = item.Quantity,
                    unitPrice = item.UnitPrice     // Add unit price
                }),
                latestPayment = order.Payments.FirstOrDefault() == null ? null : new
                {
                    method = order.Payments.FirstOrDefault().PaymentMethod,
                    status = order.Payments.FirstOrDefault().PaymentStatus.ToString(),
                    amount = order.Payments.FirstOrDefault().Amount,
                    paidAt = order.Payments.FirstOrDefault().CompletedAt
                }
            })
        });
    }
}



