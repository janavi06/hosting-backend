

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
    // In OrderController.cs

    [HttpPost("generate")]
    public async Task<ActionResult> GenerateOrder(
        [FromQuery] int restaurantId,
        [FromQuery] int? tableNo = null,
        [FromQuery] string source = "QR",
        [FromQuery] string paymentPreference = "PayLater",
        [FromBody] Order? orderData = null)
    {
        if (restaurantId <= 0)
            return BadRequest("restaurantId is required");

        if (!await _context.Restaurants.AnyAsync(r => r.RestaurantID == restaurantId))
            return BadRequest("Unknown restaurantId");

        if (orderData == null) orderData = new Order();

        if (orderData.UserID <= 0)
        {
            var anon = new User
            {
                UserRole = "customer",
                UserName = "Guest",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                UpdatedBy = "System",
                IsAvailable = true,
                RestaurantID = restaurantId
            };
            _context.Users.Add(anon);
            await _context.SaveChangesAsync();
            orderData.UserID = anon.UserID;
        }

        int? restaurantTableId = null;
        if (tableNo.HasValue)
        {
            var table = await _context.RestaurantTables
                .FirstOrDefaultAsync(t => t.RestaurantTableID == tableNo && t.RestaurantID == restaurantId);
            if (table == null)
                return BadRequest("Table does not belong to this restaurant");
            restaurantTableId = table.RestaurantTableID;
        }

        OrderSource orderSource;
        if (source.Equals("waiter", StringComparison.OrdinalIgnoreCase))
        {
            orderSource = OrderSource.Waiter;
        }
        else
        {
            orderSource = OrderSource.QR;
        }

        var order = new Order
        {
            UserID = orderData.UserID,
            RestaurantID = restaurantId,
            RestaurantTableID = restaurantTableId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = orderData.UserID.ToString(),
            UpdatedBy = orderData.UserID.ToString(),
            OrderStatus = OrderStatus.Pending,
            KitchenStatus = KitchenStatus.Pending,
            Source = orderSource,
            OrderItems = new List<OrderItem>()
        };

        if (orderData.OrderItems != null && orderData.OrderItems.Any())
        {
            foreach (var inc in orderData.OrderItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductID = inc.ProductID,
                    Quantity = inc.Quantity,
                    UnitPrice = inc.UnitPrice,
                    BatchID = 1,
                    RestaurantID = restaurantId,
                    IsPrepared = false,
                    AddedToKitchenAt = DateTime.UtcNow,
                    Customizations = (inc.CustomizationOptionIds != null ? inc.CustomizationOptionIds : new List<int>())
                        .Select(id => new OrderItemCustomization
                        {
                            CustomizationOptionID = id,
                            RestaurantID = restaurantId // ✅ ADD THIS

                        }).ToList()
                });
            }
            _orderRepository.CalculateOrderAmounts(order);
        }

        int? waiterId = await _orderRepository.GetNextAvailableWaiterAsync(restaurantId);
        if (waiterId.HasValue)
        {
            order.WaiterUserID = waiterId.Value;
            order.IsAssigned = true;
        }

        var created = await _orderRepository.AddOrderAsync(order);

        // ✅ FIX: Only create a pending payment record for the 'PayLater' flow.
        // For 'PayNow', the frontend will call payment initiation endpoints separately after this.
        if (source.Equals("waiter", StringComparison.OrdinalIgnoreCase) &&
            paymentPreference.Equals("PayLater", StringComparison.OrdinalIgnoreCase))
        {
            var payment = new Payment
            {
                OrderID = created.OrderID,
                TableNo = order.RestaurantTableID ?? 0,
                Amount = created.TotalAmount,
                PaymentMethod = "Deferred",
                PaymentStatus = PaymentStatus.Pending,
                PaymentChannel = PaymentChannel.Waiter,
                RestaurantID = restaurantId,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = null // It's a pending payment
            };
            _context.Payments.Add(payment);
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Order created successfully",
            orderID = created.OrderID,
            source = created.Source.ToString(),
            waiterID = created.WaiterUserID,
            paymentStatus = paymentPreference.Equals("PayLater", StringComparison.OrdinalIgnoreCase) ? "pending" : "created",
            orderStatus = created.OrderStatus.ToString(),
            orderItems = created.OrderItems.Select(i => new
            {
                i.ProductID,
                i.Quantity,
                i.UnitPrice
            })
        });
    }


    [HttpPost("{orderId}/addItem")]
    public async Task<IActionResult> AddItemsToCart(int orderId, [FromQuery] int restaurantId, [FromBody] List<OrderItem> orderItems)
    {
        try
        {
            _logger.LogInformation("📦 Incoming OrderItems JSON: " + System.Text.Json.JsonSerializer.Serialize(orderItems));

            var existingOrder = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);
            if (existingOrder == null)
                return NotFound(new { message = "Order not found." });

            // If order is already completed or served, start a new one
            if (existingOrder.OrderStatus == OrderStatus.Completed || existingOrder.OrderStatus == OrderStatus.Served)
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
                    RestaurantID = existingOrder.RestaurantID,
                    OrderItems = new List<OrderItem>()
                };

                int? waiterId = await _orderRepository.GetNextAvailableWaiterAsync(restaurantId);
                if (waiterId.HasValue)
                {
                    newOrder.WaiterUserID = waiterId.Value;
                    newOrder.IsAssigned = true;
                }

                foreach (var incoming in orderItems)
                {
                    _logger.LogInformation($"➤ NEW ORDER ITEM — ProductID: {incoming.ProductID}, Qty: {incoming.Quantity}");

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
                        RestaurantID = restaurantId,
                        Customizations = new List<OrderItemCustomization>()
                    };

                    if (incoming.CustomizationOptionIds != null)
                    {
                        foreach (var optId in incoming.CustomizationOptionIds)
                        {
                            newItem.Customizations.Add(new OrderItemCustomization
                            {
                                CustomizationOptionID = optId,
                                RestaurantID = restaurantId // ✅ FIX: Added RestaurantID
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

            // Continue existing order
            if (existingOrder.KitchenStatus == KitchenStatus.Ready)
                existingOrder.KitchenStatus = KitchenStatus.Pending;

            int maxBatchId = existingOrder.OrderItems.Any()
                ? existingOrder.OrderItems.Max(oi => oi.BatchID)
                : 0;
            int newBatchId = maxBatchId + 1;

            foreach (var incoming in orderItems)
            {
                _logger.LogInformation($"➤ EXISTING ORDER ITEM — ProductID: {incoming.ProductID}, Qty: {incoming.Quantity}");

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
                    RestaurantID = restaurantId,
                    Customizations = new List<OrderItemCustomization>()
                };

                if (incoming.CustomizationOptionIds != null)
                {
                    foreach (var optId in incoming.CustomizationOptionIds)
                    {
                        newItem.Customizations.Add(new OrderItemCustomization
                        {
                            CustomizationOptionID = optId,
                            RestaurantID = restaurantId // ✅ FIX: Added RestaurantID
                        });
                    }
                }

                existingOrder.OrderItems.Add(newItem);
            }

            _orderRepository.CalculateOrderAmounts(existingOrder);
            await _orderRepository.UpdateOrderAsync(existingOrder);
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
    public async Task<IActionResult> UpdateOrderSummary(int orderId, [FromQuery] int restaurantId)
    {
        try
        {
            var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);
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
    public async Task<ActionResult<IEnumerable<OrderItem>>> GetCartItems(int id, [FromQuery] int restaurantId)

    {
        try
        {
            var order = await _orderRepository.GetOrderByIdWithItemsAsync(id, restaurantId);
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
    public async Task<ActionResult> GetOrderSummary(int id, [FromQuery] int restaurantId)
    {
        try
        {
            _logger.LogInformation($"Fetching order summary for OrderID: {id}, RestaurantID: {restaurantId}");

            var order = await _orderRepository.GetOrderByIdWithItemsAsync(id, restaurantId);
            if (order == null)
            {
                // Check if order exists at all
                var orderExists = await _context.Orders.AnyAsync(o => o.OrderID == id);

                if (!orderExists)
                {
                    _logger.LogWarning($"Order {id} doesn't exist in database");
                    return NotFound(new
                    {
                        message = $"Order {id} doesn't exist",
                        suggestion = "Check the order ID or create a new order"
                    });
                }

                // Check if order exists for different restaurant
                var actualRestaurantId = await _context.Orders
                    .Where(o => o.OrderID == id)
                    .Select(o => o.RestaurantID)
                    .FirstOrDefaultAsync();

                _logger.LogWarning($"Order {id} exists but belongs to restaurant {actualRestaurantId} (requested: {restaurantId})");
                return NotFound(new
                {
                    message = $"Order {id} belongs to a different restaurant",
                    actualRestaurantId,
                    requestedRestaurantId = restaurantId
                });
            }

            _orderRepository.CalculateOrderAmounts(order);

            var response = new
            {
                message = "Order summary fetched successfully!",
                orderID = order.OrderID,
                orderStatus = order.OrderStatus.ToString(),
                createdAt = order.CreatedAt,
                orderItems = order.OrderItems.Select(item => new

                {
                    productID = item.ProductID,
                    productName = item.Product?.ProductName,
                    quantity = item.Quantity,
                    unitPrice = item.UnitPrice,
                    lineTotal = item.UnitPrice * item.Quantity,

                    customizations = item.Customizations.Select(c => new
                    {
                        c.CustomizationOptionID,
                        c.CustomizationOption.Name,
                        c.CustomizationOption.FixedPrice

                    })
                }),
                subtotal = order.Subtotal,
                discountAmount = order.DiscountAmount,
                appliedOffer = order.AppliedOffer != null ? new
                {
                    offerID = order.AppliedOffer.OfferID,
                    description = order.AppliedOffer.Description,
                    discountType = order.AppliedOffer.DiscountAmount.HasValue ? "Flat" : "Percent",
                    discountValue = order.AppliedOffer.DiscountAmount ?? (decimal?)order.AppliedOffer.DiscountPercent
                } : null,
                cgst = order.CGST,
                sgst = order.SGST,
                serviceCharge = order.ServiceCharge,
                totalAmount = order.TotalAmount
            };

            _logger.LogInformation($"Successfully retrieved order {id} summary");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching order {id} summary for restaurant {restaurantId}");
            return StatusCode(500, new
            {
                message = "An error occurred while fetching the order summary.",
                details = ex.Message
            });
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
    public async Task<IActionResult> ConfirmOrder(int orderId, [FromQuery] int restaurantId)
    {
        try
        {
            var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);
            if (order == null || order.RestaurantID != restaurantId)
                return NotFound(new { message = "Order not found for this restaurant.", orderId });

            if (order.OrderStatus != OrderStatus.Pending)
            {
                return BadRequest(new
                {
                    message = $"Order is already {order.OrderStatus}",
                    currentStatus = order.OrderStatus.ToString()
                });
            }

            order.OrderStatus = OrderStatus.Confirmed;
            order.KitchenStatus = KitchenStatus.Pending;
            order.UpdatedAt = DateTime.UtcNow;

            var updatedOrder = await _orderRepository.UpdateOrderAsync(order);
            if (updatedOrder == null)
            {
                _logger.LogError($"Failed to update OrderID: {orderId}");
                return StatusCode(500, new { message = "Failed to update the order.", orderId });
            }

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
    public async Task<IActionResult> GetPendingKitchenOrders([FromQuery] int restaurantId)
    {
        var allUnpreparedItems = await _context.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.Product)
            .Include(oi => oi.Customizations).ThenInclude(c => c.CustomizationOption)
            .Where(oi =>
                oi.Order.OrderStatus == OrderStatus.Confirmed &&
                oi.Order.RestaurantID == restaurantId && // ✅ Restaurant filter
                oi.Product != null && oi.Order != null)
            .ToListAsync();

        var grouped = allUnpreparedItems
            .GroupBy(oi => new { oi.OrderID, oi.BatchID })
            .Select(group =>
            {
                var firstItem = group.First();
                var order = firstItem.Order;

                var batchKitchenStatus = group.All(x => x.IsPrepared) ? KitchenStatus.Ready :
                                         group.Any(x => x.IsPrepared) ? KitchenStatus.Preparing :
                                         KitchenStatus.Pending;

                return new
                {
                    orderID = group.Key.OrderID,
                    batchID = group.Key.BatchID,
                    restaurantTableID = order.RestaurantTableID,
                    createdAt = group.Max(x => x.AddedToKitchenAt),
                    playSound = group.Any(x => !x.IsPrepared),
                    lastKitchenReadyAt = order.LastKitchenReadyAt,
                    kitchenStatus = batchKitchenStatus,
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
            .Where(group => group.items.Any(i => !i.isPrepared))
            .OrderBy(g => g.createdAt)
            .ToList();

        return Ok(new
        {
            message = "Pending kitchen orders fetched successfully",
            orders = grouped
        });
    }

    [HttpGet("kitchen/history-orders")]
    public async Task<IActionResult> GetKitchenHistoryOrders([FromQuery] int restaurantId)
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Where(o => o.KitchenStatus == KitchenStatus.Ready && o.RestaurantID == restaurantId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var result = orders.Select(o => new
        {
            o.OrderID,
            o.RestaurantTableID,
            o.KitchenStatus,
            CreatedAt = o.CreatedAt,
            Items = o.OrderItems.Select(oi => new
            {
                ProductID = oi.ProductID,
                Name = oi.Product.ProductName,
                oi.Quantity,
                Customizations = oi.Customizations.Select(c => new
                {
                    c.CustomizationOptionID,
                    OptionName = c.CustomizationOption.Name
                }).ToList()
            }).ToList()
        });

        return Ok(new { orders = result });
    }



    [HttpPut("kitchen/update-batch-status/{orderId}")]
    public async Task<IActionResult> UpdateBatchStatus(int orderId, [FromBody] JsonElement payload, [FromQuery] int restaurantId)
    {
        try
        {
            if (!payload.TryGetProperty("status", out var statusProp) ||
                !payload.TryGetProperty("batchID", out var batchProp))
                return BadRequest("Missing status or batchID.");

            string status = statusProp.GetString()?.Trim().ToLower();
            int batchId = batchProp.GetInt32();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);

            if (order == null)
                return NotFound("Order not found.");

            var itemsInBatch = order.OrderItems.Where(oi => oi.BatchID == batchId).ToList();
            if (!itemsInBatch.Any())
                return NotFound("No items found in this batch.");

            if (status == "preparing")
            {
                foreach (var item in itemsInBatch)
                    item.IsPrepared = false;

                order.KitchenStatus = KitchenStatus.Preparing;
            }
            else if (status == "ready")
            {
                foreach (var item in itemsInBatch)
                {
                    item.IsPrepared = true;
                    item.PreparedAt = DateTime.UtcNow;
                }

                order.KitchenStatus = KitchenStatus.Ready;
                order.LastKitchenReadyAt = DateTime.UtcNow;

                // ✅ FIX: Handle nullable table number in notification
                int tableNo = order.RestaurantTableID.HasValue ? order.RestaurantTableID.Value : 0;

                _context.WaiterNotifications.Add(new WaiterNotification
                {
                    OrderId = orderId,
                    TableNo = tableNo, // ✅ Properly converted from nullable to non-nullable
                    Message = $"Order #{orderId} for Table {tableNo} is ready",
                    CreatedAt = DateTime.UtcNow,
                    IsAcknowledged = false,
                    RestaurantID = order.RestaurantID
                });
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
    public async Task<IActionResult> MarkOrderReady(int orderId, [FromQuery] int restaurantId)
    {
        var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);
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

        // ✅ FIX: Handle nullable table number in notification
        int tableNoForNotification = order.RestaurantTableID.HasValue ? order.RestaurantTableID.Value : 0;

        var notification = new WaiterNotification
        {
            OrderId = orderId,
            TableNo = tableNoForNotification, // ✅ Properly converted
            Message = $"Order #{orderId} for Table {tableNoForNotification} is ready",
            CreatedAt = now,
            IsAcknowledged = false,
            RestaurantID = order.RestaurantID
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

            // ✅ FIX: Handle nullable table number in notification
            int tableNo = order.RestaurantTableID.HasValue ? order.RestaurantTableID.Value : 0;

            // Create notification for waiter
            var notification = new WaiterNotification
            {
                OrderId = orderId,
                TableNo = tableNo, // ✅ Properly converted from nullable to non-nullable
                Message = $"Order #{orderId} for Table {tableNo} is ready",
                CreatedAt = DateTime.UtcNow,
                IsAcknowledged = false,
                RestaurantID = order.RestaurantID // ✅ Added missing RestaurantID
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
    public async Task<IActionResult> GetWaiterNotifications([FromQuery] int restaurantId)
    {
        var notifications = await _context.WaiterNotifications
            .Where(n => !n.IsAcknowledged)
            .Join(_context.Orders,
                  n => n.OrderId,
                  o => o.OrderID,
                  (n, o) => new { n, o })
            .Where(joined => joined.o.RestaurantID == restaurantId)
            .Select(joined => joined.n)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return Ok(notifications);
    }


    [HttpPut("waiter/notifications/{notificationId}/acknowledge")]
    public async Task<IActionResult> AcknowledgeNotification(int notificationId)
    {
        var notification = await _context.WaiterNotifications.FindAsync(notificationId);
        if (notification == null)
            return NotFound();

        notification.IsAcknowledged = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }





    [HttpPut("{orderId}/serve")]
    public async Task<IActionResult> ServeOrder(int orderId, [FromQuery] int restaurantId)
    {
        try
        {
            var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);
            if (order == null || order.RestaurantID != restaurantId)
                return NotFound(new { message = "Order not found for this restaurant." });

            // ✅ MODIFICATION: Allow serving if the order is confirmed, regardless of kitchen status.
            if (order.OrderStatus != OrderStatus.Confirmed)
            {
                return BadRequest(new { message = $"Order cannot be served from its current state: {order.OrderStatus}." });
            }

            order.OrderStatus = OrderStatus.Served;
            order.UpdatedAt = DateTime.UtcNow;
            // The order will now be closed when payment is completed.
            // order.ClosedAt = DateTime.UtcNow; 

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
    public async Task<IActionResult> CompleteOrder(int orderId, [FromQuery] int restaurantId)
    {
        try
        {
            var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);
            if (order == null || order.RestaurantID != restaurantId)
                return NotFound(new { message = "Order not found for this restaurant." });

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
    public async Task<IActionResult> CancelOrder(int orderId, [FromQuery] int restaurantId)
    {
        try
        {
            var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);
            if (order == null || order.RestaurantID != restaurantId)
                return NotFound(new { message = "Order not found for this restaurant." });

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

        // Assuming accept means delete/resolve
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
    public async Task<IActionResult> AssignWaiterToOrder(int orderId, int waiterId, [FromQuery] int restaurantId)
    {
        try
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);

            if (order == null)
                return NotFound(new { message = "Order not found for this restaurant." });

            var waiter = await _context.Users.FindAsync(waiterId);
            if (waiter == null || waiter.UserRole.ToLower() != "waiter")
                return NotFound(new { message = "Waiter not found." });

            order.WaiterUserID = waiterId;
            order.IsAssigned = true;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Waiter {waiterId} assigned to Order {orderId} successfully!" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error assigning waiter: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "An error occurred while assigning the waiter.");
        }
    }


    [HttpGet("waiter-requests")]
    public async Task<IActionResult> GetWaiterRequests([FromQuery] int restaurantId)
    {
        var requests = await _context.WaiterRequests
            .Where(r => r.RestaurantID == restaurantId) // <-- Add Filter
            .OrderByDescending(r => r.RequestTime)
            .ToListAsync();

        return Ok(new { data = requests });
    }

    [HttpPost("uploadImage")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage([FromForm] UploadImageRequest request)
    {
        var file = request.File;
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(fileStream);

        return Ok(new { imagePath = $"/uploads/{uniqueFileName}" });
    }


    [HttpPost("rate-order")]
    public async Task<IActionResult> RateOrder([FromBody] Review review, [FromQuery] int restaurantId)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderID == review.OrderID && o.RestaurantID == restaurantId);

        if (order == null)
            return NotFound(new { message = $"Order {review.OrderID} not found for this restaurant." });

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
              .ThenInclude(oi => oi.Customizations)
                .ThenInclude(c => c.CustomizationOption) // ✅ Include customizations
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

                            // ✅ Add customizations to item name if they exist
                            if (item.Customizations.Any())
                            {
                                var customizationNames = string.Join(", ", item.Customizations.Select(c => c.CustomizationOption.Name));
                                table.Cell().Text(text =>
                                {
                                    text.Span(item.Product?.ProductName ?? "Unknown");
                                    text.EmptyLine();
                                    text.Span($"Custom: {customizationNames}").FontColor(Colors.Grey.Medium).FontSize(8);
                                });
                            }
                            else
                            {
                                table.Cell().Text(item.Product?.ProductName ?? "Unknown");
                            }

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
    public async Task<IActionResult> CallWaiter([FromBody] WaiterRequest request, [FromQuery] int restaurantId)
    {
        if (request == null || string.IsNullOrEmpty(request.Message))
            return BadRequest("Invalid waiter request");

        var table = await _context.RestaurantTables
            .FirstOrDefaultAsync(t => t.RestaurantTableID == request.RestaurantTableID && t.RestaurantID == restaurantId);

        if (table == null)
            return BadRequest("Invalid table for this restaurant.");

        var restaurantExists = await _context.Restaurants.AnyAsync(r => r.RestaurantID == restaurantId);
        if (!restaurantExists)
            return BadRequest("Invalid restaurant ID.");

        request.RestaurantID = restaurantId;
        request.RequestTime = DateTime.UtcNow;
        request.IsNotified = false;

        _context.WaiterRequests.Add(request);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Waiter request sent successfully", data = request });
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
    public async Task<IActionResult> CreatePendingPayment(int orderId, [FromQuery] int restaurantId, [FromBody] dynamic payload)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);

        if (order == null)
            return NotFound();

        _orderRepository.CalculateOrderAmounts(order);
        await _context.SaveChangesAsync();

        string method = "Cash";
        if (payload is JsonElement jsonElement && jsonElement.TryGetProperty("method", out var methodProp))
            method = methodProp.GetString() ?? "Cash";

        // ✅ FIX: Handle nullable table number
        int tableNo = order.RestaurantTableID.HasValue ? order.RestaurantTableID.Value : 0;

        var payment = new Payment
        {
            OrderID = orderId,
            TableNo = tableNo, // ✅ Properly converted
            PaymentMethod = method,
            PaymentStatus = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            IsNotified = false,
            Amount = order.TotalAmount,
            RestaurantID = restaurantId
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
    public async Task<IActionResult> ClearPendingPayment(int id, [FromQuery] int restaurantId)
    {
        var payment = await _context.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.PaymentID == id && p.Order.RestaurantID == restaurantId);

        if (payment == null)
            return NotFound(new { message = "Payment not found for this restaurant." });

        payment.PaymentStatus = PaymentStatus.Success;
        payment.CompletedAt = DateTime.UtcNow;

        if (payment.Order != null)
        {
            // ✅ KEY CHANGE: Update status to Confirmed instead of Completed.
            // This keeps the order in the active "Orders" list for the waiter.
            payment.Order.OrderStatus = OrderStatus.Confirmed;

            // ✅ KEY CHANGE: Do not set ClosedAt. The order is paid but not yet completed.
            // It will be completed after being served.
            // payment.Order.ClosedAt = DateTime.UtcNow; // This line is removed.
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "✅ Payment completed and order confirmed successfully." });
    }


    [HttpPost("{orderId}/initiate-payment")]
    public async Task<IActionResult> InitiatePayment(
    int orderId,
    [FromQuery] string method = "UPI",
    [FromQuery] int restaurantId = 0,
    [FromQuery] string channel = "Customer")
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.RestaurantTable)
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);

            if (order == null)
                return NotFound(new { message = "Order not found for this restaurant." });

            _orderRepository.CalculateOrderAmounts(order);
            await _context.SaveChangesAsync();

            var transactionId = $"DIGIEAT_{orderId}_{DateTime.Now:yyyyMMddHHmmss}";

            // ✅ FIX: Properly convert channel string to PaymentChannel enum
            PaymentChannel paymentChannelEnum;
            if (channel.Equals("Waiter", StringComparison.OrdinalIgnoreCase))
            {
                paymentChannelEnum = PaymentChannel.Waiter;
            }
            else
            {
                paymentChannelEnum = PaymentChannel.Customer;
            }

            // ✅ FIX: Handle nullable table number properly
            int tableNo = order.RestaurantTableID.HasValue ? order.RestaurantTableID.Value : 0;

            var payment = new Payment
            {
                OrderID = orderId,
                TableNo = tableNo, // ✅ Now properly converted from nullable to non-nullable
                Amount = order.TotalAmount,
                PaymentMethod = method,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                PaymentChannel = paymentChannelEnum,
                RestaurantID = restaurantId
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            if (method.Equals("UPI", StringComparison.OrdinalIgnoreCase))
            {
                var restaurant = await _context.Restaurants
                    .FirstOrDefaultAsync(r => r.RestaurantID == restaurantId);

                return Ok(new
                {
                    method = "UPI",
                    upiId = restaurant?.UPI_ID,
                    upiName = restaurant?.UPI_Name ?? restaurant?.Name,
                    amount = order.TotalAmount,
                    transactionId,
                    orderId,
                    paymentId = payment.PaymentID
                });
            }

            return Ok(new
            {
                method,
                message = "Payment initiated successfully!",
                paymentId = payment.PaymentID,
                orderId = payment.OrderID,
                amount = payment.Amount,
                status = payment.PaymentStatus.ToString(),
                channel = payment.PaymentChannel.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error initiating payment: {ex.Message}");
            return StatusCode(500, new { message = "Error initiating payment.", error = ex.Message });
        }
    }

    [HttpGet("pending-payments")]
    public async Task<IActionResult> GetPendingPayments([FromQuery] int restaurantId, [FromQuery] string? channel = null)
    {
        try
        {
            var query = _context.Payments
              .Include(p => p.Order)
                .ThenInclude(o => o.OrderItems)
                  .ThenInclude(oi => oi.Product)
                      // ✅ FIX: Added critical filter to only get payments for the logged-in waiter's restaurant
                      .Where(p => p.RestaurantID == restaurantId && p.PaymentStatus == PaymentStatus.Pending);

            if (!string.IsNullOrEmpty(channel))
            {
                var paymentChannel = channel.Equals("Waiter", StringComparison.OrdinalIgnoreCase)
                  ? PaymentChannel.Waiter
                  : PaymentChannel.Customer;

                query = query.Where(p => p.PaymentChannel == paymentChannel);
            }

            var payments = await query
              .OrderByDescending(p => p.CreatedAt)
              .ToListAsync();

            return Ok(payments.Select(p => new
            {
                // Standardizing on `paymentID` to match frontend model usage
                paymentID = p.PaymentID,
                orderID = p.OrderID,
                tableNo = p.TableNo,
                status = p.PaymentStatus.ToString(),
                method = p.PaymentMethod,
                amount = p.Amount,
                // ✅ FIX: Changed property name from `channel` to `paymentChannel` to match frontend
                paymentChannel = p.PaymentChannel,
                // ✅ FIX: Added `source` from the order to make frontend logic more robust
                source = p.Order?.Source.ToString(),
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
    public async Task<IActionResult> GetSalesAnalyticsReport(
        DateTime? startDate,
        DateTime? endDate,
        [FromQuery] int restaurantId)
    {
        try
        {
            endDate ??= DateTime.UtcNow;
            startDate ??= endDate.Value.AddDays(-30);

            var orders = await _context.Orders
                .Where(o => o.RestaurantID == restaurantId &&
                            o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .ToListAsync();

            var totalRevenue = orders.SelectMany(o => o.Payments).Sum(p => p.Amount);
            var orderCount = orders.Count;
            var avgOrderValue = orderCount > 0 ? totalRevenue / orderCount : 0;

            return Ok(new
            {
                totalOrders = orderCount,
                totalRevenue,
                avgOrderValue
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sales analytics report.");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }


    [HttpGet("report/item-summary")]
    public async Task<IActionResult> GetItemSummaryReport(
        DateTime? startDate,
        DateTime? endDate,
        [FromQuery] int restaurantId)
    {
        try
        {
            endDate ??= DateTime.UtcNow;
            startDate ??= endDate.Value.AddDays(-30);

            var items = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.RestaurantID == restaurantId &&
                             oi.CreatedAt >= startDate && oi.CreatedAt <= endDate)
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

            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating item summary report.");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }


    [HttpGet("report/category-revenue")]
    public async Task<IActionResult> GetCategoryRevenueReport(
        DateTime? startDate,
        DateTime? endDate,
        [FromQuery] int restaurantId)
    {
        try
        {
            endDate ??= DateTime.UtcNow;
            startDate ??= endDate.Value.AddDays(-30);

            var result = await _context.OrderItems
                .Include(oi => oi.Product).ThenInclude(p => p.SubCategory).ThenInclude(sc => sc.Category)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.RestaurantID == restaurantId &&
                             oi.CreatedAt >= startDate && oi.CreatedAt <= endDate)
                .GroupBy(oi => oi.Product.SubCategory.Category.CategoryName)
                .Select(g => new
                {
                    category = g.Key,
                    revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .ToListAsync();

            return Ok(result);
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
    public async Task<IActionResult> GetOrderSummaryReport(
        DateTime? startDate,
        DateTime? endDate,
        [FromQuery] int restaurantId)
    {
        try
        {
            endDate ??= DateTime.UtcNow;
            startDate ??= endDate.Value.AddDays(-30);

            var orders = await _context.Orders
                .Include(o => o.Payments)
                .Where(o => o.RestaurantID == restaurantId &&
                            o.OrderStatus != OrderStatus.Cancelled &&
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
                });

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
    public async Task<IActionResult> GetMergedBills(
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end,
        [FromQuery] int restaurantId)
    {
        start = start.HasValue ? start.Value.ToUniversalTime() : null;
        end = end.HasValue ? end.Value.ToUniversalTime() : null;

        var orders = await _context.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Where(o => o.RestaurantID == restaurantId &&
                        o.OrderStatus != OrderStatus.Cancelled &&
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


    // In OrderController.cs

    [HttpGet("report/sales-analytics")]
    public async Task<IActionResult> GetSalesAnalyticsReport(
        [FromQuery] int restaurantId,
        [FromQuery] string reportType,
        [FromQuery] string timeRange,
        [FromQuery] string? compareWith = "none",
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        try
        {
            // Step 1: Determine the date range from the 'timeRange' parameter
            var (currentStart, currentEnd) = GetDateRange(timeRange, startDate, endDate);

            // Step 2: Get the data for the main period
            var allOrdersInPeriod = await _context.Orders
                .Where(o => o.RestaurantID == restaurantId && o.CreatedAt >= currentStart && o.CreatedAt <= currentEnd)
                .ToListAsync();

            var completedOrders = allOrdersInPeriod.Where(o => o.OrderStatus == OrderStatus.Completed).ToList();

            // Step 3: Calculate main KPIs
            decimal totalRevenue = completedOrders.Sum(o => o.TotalAmount);
            int totalOrders = allOrdersInPeriod.Count;
            int totalCancellations = allOrdersInPeriod.Count(o => o.OrderStatus == OrderStatus.Cancelled);
            decimal avgOrderValue = completedOrders.Any() ? completedOrders.Average(o => o.TotalAmount) : 0;
            decimal cancellationRate = totalOrders > 0 ? (decimal)totalCancellations / totalOrders : 0;

            // Step 4: Fetch detailed data and build the anonymous object for the response

            var dailyData = allOrdersInPeriod
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Where(o => o.OrderStatus == OrderStatus.Completed).Sum(o => o.TotalAmount),
                    AvgOrderValue = g.Where(o => o.OrderStatus == OrderStatus.Completed).Any() ? g.Where(o => o.OrderStatus == OrderStatus.Completed).Average(o => o.TotalAmount) : 0,
                    Cancellations = g.Count(o => o.OrderStatus == OrderStatus.Cancelled),
                    CancellationRate = g.Any() ? (decimal)g.Count(o => o.OrderStatus == OrderStatus.Cancelled) / g.Count() : 0
                }).ToList();

            var paymentMethods = await _context.Payments
                .Where(p => p.RestaurantID == restaurantId && p.CreatedAt >= currentStart && p.CreatedAt <= currentEnd)
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new { Method = g.Key, Count = g.Count(), Amount = g.Sum(p => p.Amount) })
                .ToListAsync();

            var topTables = completedOrders
                .Where(o => o.RestaurantTableID.HasValue)
                .GroupBy(o => o.RestaurantTableID)
                .Select(g => new
                {
                    TableNo = g.Key.Value,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount),
                    AvgValue = g.Average(o => o.TotalAmount)
                })
                .OrderByDescending(t => t.Revenue)
                .Take(5)
                .ToList();

            var hourlyData = completedOrders
                .GroupBy(o => o.CreatedAt.Hour)
                .Select(g => new
                {
                    Hour = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(h => h.Hour)
                .ToList();

            var allItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.Order.RestaurantID == restaurantId && oi.Order.CreatedAt >= currentStart && oi.Order.CreatedAt <= currentEnd)
                .GroupBy(oi => oi.Product.ProductName)
                .Select(g => new { ProductName = g.Key, Quantity = g.Sum(oi => oi.Quantity), Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice) })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            var categoryPerformance = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product).ThenInclude(p => p.Category)
                .Where(oi => oi.Order.RestaurantID == restaurantId && oi.Order.CreatedAt >= currentStart && oi.Order.CreatedAt <= currentEnd && oi.Product.Category != null)
                .GroupBy(oi => oi.Product.Category.CategoryName)
                .Select(g => new
                {
                    CategoryName = g.Key,
                    Quantity = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice),
                    Percentage = totalRevenue > 0 ? g.Sum(oi => oi.Quantity * oi.UnitPrice) / totalRevenue : 0,
                    AvgPrice = g.Sum(oi => oi.Quantity) > 0 ? g.Sum(oi => oi.Quantity * oi.UnitPrice) / g.Sum(oi => oi.Quantity) : 0
                })
                .OrderByDescending(c => c.Revenue)
                .ToListAsync();

            // Step 5: Return the final, structured anonymous object
            return Ok(new
            {
                totalRevenue,
                totalOrders,
                avgOrderValue,
                cancellationRate,
                revenueChange = 0, // Placeholder for comparison logic
                orderChange = 0,   // Placeholder for comparison logic
                aovChange = 0,     // Placeholder for comparison logic
                cancellationChange = 0, // Placeholder for comparison logic
                totalCancellations,
                dailyData,
                paymentMethods,
                topTables,
                hourlyData,
                topItems = allItems.Take(10),
                bottomItems = allItems.TakeLast(10).OrderBy(i => i.Revenue),
                categoryPerformance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating sales analytics report: {ex.Message}");
            // Return an empty object on error so the frontend doesn't crash
            return Ok(new
            {
                totalRevenue = 0,
                totalOrders = 0,
                avgOrderValue = 0,
                cancellationRate = 0,
                dailyData = new List<object>(),
                paymentMethods = new List<object>(),
                topTables = new List<object>(),
                hourlyData = new List<object>(),
                topItems = new List<object>(),
                bottomItems = new List<object>(),
                categoryPerformance = new List<object>()
            });
        }
    }

    // You still need this helper function in your controller
    private (DateTime, DateTime) GetDateRange(string timeRange, string? customStart, string? customEnd)
    {
        // Make sure this helper function exists from the previous answer
        DateTime today = DateTime.Today.ToUniversalTime();
        DateTime startDate = today;
        DateTime endDate = today.AddDays(1).AddSeconds(-1);

        switch (timeRange.ToLower())
        {
            case "today":
                // Default is already today
                break;
            case "yesterday":
                startDate = today.AddDays(-1);
                endDate = today.AddSeconds(-1);
                break;
            case "last7":
                startDate = today.AddDays(-6);
                break;
            case "last30":
                startDate = today.AddDays(-29);
                break;
            case "thismonth":
                startDate = new DateTime(today.Year, today.Month, 1).ToUniversalTime();
                break;
            case "lastmonth":
                var lastMonth = today.AddMonths(-1);
                startDate = new DateTime(lastMonth.Year, lastMonth.Month, 1).ToUniversalTime();
                endDate = startDate.AddMonths(1).AddSeconds(-1);
                break;
            case "custom":
                if (!string.IsNullOrEmpty(customStart) && !string.IsNullOrEmpty(customEnd))
                {
                    startDate = DateTime.Parse(customStart).ToUniversalTime();
                    endDate = DateTime.Parse(customEnd).ToUniversalTime().AddDays(1).AddSeconds(-1);
                }
                break;
        }
        return (startDate, endDate);
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
    public async Task<ActionResult<IEnumerable<object>>> GetRestaurantTables([FromQuery] int restaurantId)
    {
        var tables = await _context.RestaurantTables
            .Include(t => t.Restaurant)
            .Where(t => t.RestaurantID == restaurantId)
            .Select(t => new
            {
                t.RestaurantTableID,
                t.TableName,
                t.Seats,
                t.RestaurantID,
                RestaurantName = t.Restaurant != null ? t.Restaurant.Name : null,
                t.CreatedAt,
                t.UpdatedAt
            })
            .ToListAsync();

        return Ok(tables);
    }

    [HttpGet("{orderId}/bill-html")]
    public async Task<IActionResult> GetBillHtml(int orderId, [FromQuery] int restaurantId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.RestaurantTable)
            .FirstOrDefaultAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);

        if (order == null)
            return NotFound();

        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.RestaurantID == restaurantId);
        _orderRepository.CalculateOrderAmounts(order);
        await _orderRepository.ApplyBestAvailableOfferAsync(order);
        await _context.SaveChangesAsync();

        var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

        var html = $@"<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8'>
  <title>Order Bill #{order.OrderID}</title>
  <style>
    body {{
      font-family: 'Segoe UI', sans-serif;
      padding: 20px;
      background: #fff;
    }}
    .bill-container {{ max-width: 700px; margin: auto; }}
    .restaurant-header {{ text-align: center; }}
    .restaurant-header h2 {{ margin: 0; }}
    table {{ width: 100%; margin-top: 20px; border-collapse: collapse; font-size: 14px; }}
    th, td {{ border: 1px solid #ccc; padding: 8px; text-align: left; }}
    th {{ background: #f2f2f2; }}
    .totals {{ margin-top: 20px; text-align: right; }}
    .footer {{ text-align: center; margin-top: 30px; font-size: 14px; color: #555; }}
  </style>
</head>
<body>
<div class='bill-container'>
  <div class='restaurant-header'>
    <h2>{restaurant?.Name ?? "Restaurant"}</h2>
    <p>{restaurant?.Description ?? ""}</p>
    <p>Date: {istNow:dd MMM yyyy hh:mm tt}</p>
  </div>
  <p><strong>Order ID:</strong> #{order.OrderID}</p>
  <p><strong>Table No:</strong> {order.RestaurantTable?.TableName ?? "N/A"}</p>
  <table>
    <thead><tr><th>#</th><th>Item</th><th>Qty</th><th>Rate</th><th>Total</th></tr></thead>
    <tbody>";

        int count = 1;
        foreach (var item in order.OrderItems)
        {
            var total = item.Quantity * item.UnitPrice;
            html += $"<tr><td>{count++}</td><td>{item.Product?.ProductName}</td><td>{item.Quantity}</td><td>₹{item.UnitPrice:N2}</td><td>₹{total:N2}</td></tr>";
        }

        html += $@"</tbody></table><div class='totals'>
    <p>Subtotal: ₹{order.Subtotal:N2}</p>";

        if (order.AppliedOffer != null)
            html += $"<p>Discount ({order.AppliedOffer.Description}): -₹{order.DiscountAmount:N2}</p>";

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

    // Inside OrderController.cs

    [HttpGet("dashboard/active-orders")]
    public async Task<IActionResult> GetActiveOrders([FromQuery] int restaurantId)
    {
        try
        {
            var activeOrders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.Payments.OrderByDescending(p => p.CreatedAt))
                .Include(o => o.Waiter) // Include waiter info
                .Where(o => o.RestaurantID == restaurantId &&
                            o.OrderStatus != OrderStatus.Completed &&
                            o.OrderStatus != OrderStatus.Cancelled)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            // Ensure calculations are run for totals
            foreach (var order in activeOrders)
            {
                _orderRepository.CalculateOrderAmounts(order);
            }
            await _context.SaveChangesAsync();

            var result = new
            {
                totalActiveOrders = activeOrders.Count,
                orders = activeOrders.Select(o =>
                {
                    // Determine the most relevant status for the dashboard
                    string mainStatus;
                    if (o.Payments.Any(p => p.PaymentStatus == PaymentStatus.Pending))
                    {
                        mainStatus = "Pending Payment";
                    }
                    else if (o.KitchenStatus == KitchenStatus.Ready && o.OrderStatus != OrderStatus.Served)
                    {
                        mainStatus = "Awaiting Service";
                    }
                    else if (o.OrderStatus == OrderStatus.Confirmed || o.KitchenStatus == KitchenStatus.Preparing)
                    {
                        mainStatus = "In Progress";
                    }
                    else
                    {
                        mainStatus = o.OrderStatus.ToString();
                    }

                    return new
                    {
                        orderID = o.OrderID,
                        tableNo = o.RestaurantTableID,
                        status = mainStatus,
                        items = o.OrderItems.Select(oi => new { oi.Quantity, productName = oi.Product?.ProductName }),
                        totalAmount = o.TotalAmount,
                        lastUpdated = o.UpdatedAt,
                        createdAt = o.CreatedAt, // Add createdAt for oldest pending calculation
                        waiterUserID = o.WaiterUserID,
                        waiterName = o.Waiter?.UserName // Add waiter name
                    };
                })
            };

            return Ok(new
            {
                message = "Active orders for dashboard fetched successfully.",
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching active orders: {ex.Message}");
            return StatusCode(500, "An error occurred while fetching active orders.");
        }
    }

    // Inside OrderController.cs

    [HttpGet("dashboard/kitchen-backlog")]
    public async Task<IActionResult> GetKitchenBacklog([FromQuery] int restaurantId)
    {
        try
        {
            var totalPendingItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.RestaurantID == restaurantId &&
                             oi.Order.OrderStatus == OrderStatus.Confirmed &&
                             !oi.IsPrepared)
                .SumAsync(oi => oi.Quantity);

            return Ok(new
            {
                message = "Kitchen backlog fetched successfully.",
                totalPendingItems
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching kitchen backlog: {ex.Message}");
            return StatusCode(500, "An error occurred while fetching kitchen backlog.");
        }
    }

    // Inside OrderController.cs

    [HttpGet("report/today-summary")]
    public async Task<IActionResult> GetTodaySummaryReport([FromQuery] int restaurantId)
    {
        try
        {
            var todayStart = DateTime.Today.ToUniversalTime();
            var todayEnd = todayStart.AddDays(1).AddSeconds(-1);

            var orders = await _context.Orders
                .Where(o => o.RestaurantID == restaurantId &&
                            o.CreatedAt >= todayStart && o.CreatedAt <= todayEnd)
                .ToListAsync();

            var completedOrders = orders.Where(o => o.OrderStatus == OrderStatus.Completed).ToList();
            var cancelledOrders = orders.Where(o => o.OrderStatus == OrderStatus.Cancelled).ToList();

            var totalRevenue = completedOrders.Sum(o => o.TotalAmount);
            var orderCount = completedOrders.Count;
            var avgOrderValue = orderCount > 0 ? totalRevenue / orderCount : 0;

            return Ok(new
            {
                totalOrders = orders.Count,
                totalRevenue,
                avgOrderValue,
                totalCancelled = cancelledOrders.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating today's summary report.");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    [HttpPut("{orderId}/update-item/{itemId}")]
    public async Task<IActionResult> UpdateOrderItem(int orderId, int itemId, [FromQuery] int restaurantId, [FromBody] JsonElement payload)
    {
        // Use explicit transaction to handle both operations safely
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (!payload.TryGetProperty("changedByUserId", out var changedByProp))
                return BadRequest("changedByUserId is required");

            int changedByUserId = changedByProp.GetInt32();

            var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);
            if (order == null || order.RestaurantID != restaurantId)
                return NotFound(new { message = "Order not found for this restaurant." });

            var item = order.OrderItems.FirstOrDefault(oi => oi.OrderItemID == itemId);
            if (item == null)
                return NotFound(new { message = "Order item not found." });

            string productName = item.Product?.ProductName ?? "Item";
            int oldQuantity = item.Quantity;

            // Update quantity if provided
            if (payload.TryGetProperty("quantity", out var qtyProp) && qtyProp.ValueKind == JsonValueKind.Number)
            {
                int newQuantity = qtyProp.GetInt32();

                if (newQuantity <= 0)
                {
                    order.OrderItems.Remove(item);
                }
                else
                {
                    item.Quantity = newQuantity;
                }
            }

            // Update customizations if provided
            if (payload.TryGetProperty("customizationOptionIds", out var customProp) && customProp.ValueKind == JsonValueKind.Array)
            {
                item.Customizations.Clear();
                foreach (var optElement in customProp.EnumerateArray())
                {
                    if (optElement.ValueKind == JsonValueKind.Number)
                    {
                        item.Customizations.Add(new OrderItemCustomization
                        {
                            CustomizationOptionID = optElement.GetInt32()
                        });
                    }
                }
            }

            // Reset kitchen status since items changed
            order.KitchenStatus = KitchenStatus.Pending;
            foreach (var orderItem in order.OrderItems)
            {
                orderItem.IsPrepared = false;
            }

            _orderRepository.CalculateOrderAmounts(order);
            await _orderRepository.ApplyBestAvailableOfferAsync(order);

            // ✅ FIX: Find and update any associated pending payment with the new total
            var pendingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderID == orderId && p.PaymentStatus == PaymentStatus.Pending);

            if (pendingPayment != null)
            {
                pendingPayment.Amount = order.TotalAmount;
            }

            // Save order and payment changes
            await _context.SaveChangesAsync();

            // Log the change (separate operation)
            string changeDescription = payload.TryGetProperty("quantity", out var qtyProp2) && qtyProp2.ValueKind == JsonValueKind.Number
                ? $"{productName} quantity changed from {oldQuantity} to {qtyProp2.GetInt32()}"
                : $"Updated {productName}";

            await LogOrderChange(orderId, "ITEM_UPDATED", changeDescription, changedByUserId, restaurantId);

            // Commit transaction
            await transaction.CommitAsync();

            // Notify kitchen (outside transaction)
            await NotifyKitchenOrderUpdated(orderId, "ORDER_UPDATED", restaurantId, order.RestaurantTableID);

            return Ok(new
            {
                message = "Order item updated successfully",
                orderID = order.OrderID,
                newTotal = order.TotalAmount
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError($"Error updating order item: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred while updating the order item." });
        }
    }

    [HttpPost("{orderId}/add-item")]
    public async Task<IActionResult> AddItemToOrder(int orderId, [FromQuery] int restaurantId, [FromBody] JsonElement payload)
    {
        try
        {
            if (!payload.TryGetProperty("productID", out var productProp) ||
                !payload.TryGetProperty("quantity", out var qtyProp) ||
                !payload.TryGetProperty("changedByUserId", out var changedByProp))
                return BadRequest("productID, quantity, and changedByUserId are required");

            int productId = productProp.GetInt32();
            int quantity = qtyProp.GetInt32();
            int changedByUserId = changedByProp.GetInt32();

            var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);
            if (order == null || order.RestaurantID != restaurantId)
                return NotFound(new { message = "Order not found." });

            if (order.OrderStatus == OrderStatus.Completed || order.OrderStatus == OrderStatus.Served)
                return BadRequest(new { message = "Cannot modify completed or served orders." });

            var product = await _productRepository.GetProductByIdAsync(productId);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            if (!product.IsAvailable)
                return BadRequest(new { message = "Product is not available." });

            int newBatchId = order.OrderItems.Any() ? order.OrderItems.Max(oi => oi.BatchID) + 1 : 1;

            var newItem = new OrderItem
            {
                ProductID = productId,
                Quantity = quantity,
                UnitPrice = product.Price,
                IsPrepared = false,
                AddedToKitchenAt = DateTime.UtcNow,
                BatchID = newBatchId,
                RestaurantID = restaurantId,
                Customizations = new List<OrderItemCustomization>()
            };

            if (payload.TryGetProperty("customizationOptionIds", out var customProp) && customProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var optElement in customProp.EnumerateArray())
                {
                    if (optElement.ValueKind == JsonValueKind.Number)
                    {
                        newItem.Customizations.Add(new OrderItemCustomization
                        {
                            CustomizationOptionID = optElement.GetInt32()
                        });
                    }
                }
            }

            order.OrderItems.Add(newItem);
            order.KitchenStatus = KitchenStatus.Pending;

            _orderRepository.CalculateOrderAmounts(order);
            await _orderRepository.ApplyBestAvailableOfferAsync(order);

            // ✅ FIX: Find and update any associated pending payment with the new total
            var pendingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderID == orderId && p.PaymentStatus == PaymentStatus.Pending);

            if (pendingPayment != null)
            {
                pendingPayment.Amount = order.TotalAmount;
            }

            await LogOrderChange(orderId, "ITEM_ADDED",
                $"Added {product.ProductName} (Qty: {quantity})", changedByUserId, restaurantId);

            await _context.SaveChangesAsync();

            // Notify kitchen
            await NotifyKitchenOrderUpdated(orderId, "ITEM_ADDED", restaurantId, order.RestaurantTableID);

            return Ok(new
            {
                message = "Item added to order successfully",
                orderID = order.OrderID,
                itemId = newItem.OrderItemID,
                newTotal = order.TotalAmount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding item to order: {ex.Message}");
            return StatusCode(500, "An error occurred while adding item to order.");
        }
    }

    [HttpDelete("{orderId}/cancel")]
    public async Task<IActionResult> CancelOrder(int orderId, [FromQuery] int restaurantId, [FromBody] JsonElement payload)
    {
        try
        {
            if (!payload.TryGetProperty("changedByUserId", out var changedByProp))
                return BadRequest("changedByUserId is required");

            int changedByUserId = changedByProp.GetInt32();
            string reason = "No reason provided";

            if (payload.TryGetProperty("reason", out var reasonProp))
            {
                reason = reasonProp.GetString();
            }

            var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);
            if (order == null || order.RestaurantID != restaurantId)
                return NotFound(new { message = "Order not found." });

            // Check if order can be cancelled
            if (order.OrderStatus == OrderStatus.Completed || order.OrderStatus == OrderStatus.Served)
                return BadRequest(new { message = "Cannot cancel completed or served orders." });

            order.OrderStatus = OrderStatus.Cancelled;
            order.ClosedAt = DateTime.UtcNow;
            order.KitchenStatus = KitchenStatus.Pending;

            // Cancel any pending payments - FIXED: Use your existing PaymentStatus values
            var pendingPayments = await _context.Payments
                .Where(p => p.OrderID == orderId && p.PaymentStatus == PaymentStatus.Pending)
                .ToListAsync();

            foreach (var payment in pendingPayments)
            {
                // If you don't have Cancelled status, use Failed or handle differently
                payment.PaymentStatus = PaymentStatus.Failed; // Or create Cancelled status
            }

            await LogOrderChange(orderId, "ORDER_CANCELLED",
                $"Order cancelled. Reason: {reason}", changedByUserId, restaurantId);

            await _context.SaveChangesAsync();

            // Notify kitchen if order was in progress
            if (order.KitchenStatus == KitchenStatus.Preparing || order.KitchenStatus == KitchenStatus.Ready)
            {
                await NotifyKitchenOrderUpdated(orderId, "ORDER_CANCELLED", restaurantId, order.RestaurantTableID);
            }

            return Ok(new
            {
                message = "Order cancelled successfully",
                orderID = order.OrderID
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error cancelling order: {ex.Message}");
            return StatusCode(500, "An error occurred while cancelling the order.");
        }
    }

    [HttpGet("{orderId}/change-history")]
    public async Task<IActionResult> GetOrderChangeHistory(int orderId, [FromQuery] int restaurantId)
    {
        try
        {
            // Verify order belongs to restaurant
            var orderExists = await _context.Orders
                .AnyAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);

            if (!orderExists)
                return NotFound(new { message = "Order not found for this restaurant." });

            var changes = await _context.OrderChangeHistory
                .Include(och => och.ChangedByUser)
                .Where(och => och.OrderID == orderId)
                .OrderByDescending(och => och.ChangedAt)
                .ToListAsync();

            return Ok(new
            {
                orderID = orderId,
                changes = changes.Select(c => new
                {
                    changeType = c.ChangeType,
                    description = c.Description,
                    changedBy = c.ChangedByUser?.UserName ?? "System",
                    changedByUserId = c.ChangedByUserID,
                    changedAt = c.ChangedAt,
                    oldValues = c.OldValues,
                    newValues = c.NewValues
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching order change history: {ex.Message}");
            return StatusCode(500, "An error occurred while fetching order change history.");
        }
    }

    // Helper methods (add these private methods to your controller)
    private async Task LogOrderChange(int orderId, string changeType, string description, int? changedByUserId, int restaurantId, string oldValues = null, string newValues = null)
    {
        try
        {
            // ✅ FIX: Use UserID instead of Id (based on your User model)
            bool userExists = true;
            if (changedByUserId.HasValue)
            {
                userExists = await _context.Users.AnyAsync(u => u.UserID == changedByUserId.Value);
            }

            var changeLog = new OrderChangeHistory
            {
                OrderID = orderId,
                ChangeType = changeType,
                Description = description,
                ChangedByUserID = userExists ? changedByUserId : null, // Set to null if user doesn't exist
                ChangedAt = DateTime.UtcNow,
                OldValues = oldValues,
                NewValues = newValues,
                RestaurantID = restaurantId
            };

            _context.OrderChangeHistory.Add(changeLog);

            // Use a separate SaveChanges to avoid transaction issues
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to log order change: {ex.Message}");
            // Don't throw - we don't want to fail the main operation
        }
    }
    private async Task NotifyKitchenOrderUpdated(int orderId, string updateType, int restaurantId, int? tableNo = null)
    {
        try
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return;

            // ✅ FIX: Handle nullable table number
            int tableNoForNotification = tableNo.HasValue ? tableNo.Value : 0;

            var notification = new KitchenNotification
            {
                OrderId = orderId,
                TableNo = tableNoForNotification, // ✅ Properly converted
                NotificationTime = DateTime.UtcNow,
                IsAcknowledged = false,
                Message = $"Order #{orderId} has been updated - {updateType.Replace("_", " ")}",
                RestaurantID = restaurantId
            };

            _context.KitchenNotifications.Add(notification);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error notifying kitchen: {ex.Message}");
            // Don't fail the main request if notification fails
        }
    }
    private ObjectResult StatusError(int statusCode, string message)
    {
        _logger.LogError(message);
        return StatusCode(statusCode, new { message });
    }

    // Add this endpoint to handle payment initiation for both UPI and Cash
    [HttpPost("payments/initiate")]


    public async Task<IActionResult> InitiatePayment(
        [FromQuery] int orderId,
        [FromQuery] int restaurantId,
        [FromQuery] string method = "UPI",
        [FromQuery] string channel = "Customer")
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.RestaurantTable)
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);

            if (order == null)
                return NotFound(new { message = "Order not found for this restaurant." });

            _orderRepository.CalculateOrderAmounts(order);
            await _context.SaveChangesAsync();

            var transactionId = $"DIGIEAT_{orderId}_{DateTime.Now:yyyyMMddHHmmss}";

            // Convert channel to PaymentChannel enum
            PaymentChannel paymentChannelEnum = channel.Equals("Waiter", StringComparison.OrdinalIgnoreCase)
                ? PaymentChannel.Waiter
                : PaymentChannel.Customer;

            int tableNo = order.RestaurantTableID.HasValue ? order.RestaurantTableID.Value : 0;

            var payment = new Payment
            {
                OrderID = orderId,
                TableNo = tableNo,
                Amount = order.TotalAmount,
                PaymentMethod = method,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                PaymentChannel = paymentChannelEnum,
                RestaurantID = restaurantId
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            if (method.Equals("UPI", StringComparison.OrdinalIgnoreCase))
            {
                var restaurant = await _context.Restaurants
                    .FirstOrDefaultAsync(r => r.RestaurantID == restaurantId);

                if (restaurant == null || string.IsNullOrEmpty(restaurant.UPI_ID))
                    return BadRequest(new { message = "UPI not configured for this restaurant." });

                // Build UPI URI
                var upiUri = BuildUpiUri(restaurant.UPI_ID, restaurant.UPI_Name ?? restaurant.Name,
                                       order.TotalAmount, transactionId, $"Order #{orderId}");

                return Ok(new
                {
                    method = "UPI",
                    upiId = restaurant.UPI_ID,
                    upiName = restaurant.UPI_Name ?? restaurant.Name,
                    amount = order.TotalAmount,
                    transactionId,
                    orderId,
                    paymentId = payment.PaymentID,
                    upiUri = upiUri
                });
            }

            return Ok(new
            {
                method,
                message = "Payment initiated successfully!",
                paymentId = payment.PaymentID,
                orderId = payment.OrderID,
                amount = payment.Amount,
                status = payment.PaymentStatus.ToString(),
                channel = payment.PaymentChannel.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error initiating payment: {ex.Message}");
            return StatusCode(500, new { message = "Error initiating payment.", error = ex.Message });
        }
    }

    // Add this helper method to build UPI URI
    private string BuildUpiUri(string upiId, string upiName, decimal amount, string transactionId, string note)
    {
        var encodedUpiId = Uri.EscapeDataString(upiId);
        var encodedName = Uri.EscapeDataString(upiName);
        var encodedAmount = amount.ToString("F2");
        var encodedTxnId = Uri.EscapeDataString(transactionId);
        var encodedNote = Uri.EscapeDataString(note);

        return $"upi://pay?pa={encodedUpiId}&pn={encodedName}&am={encodedAmount}&tr={encodedTxnId}&tn={encodedNote}&cu=INR";
    }

    // Add this endpoint to check payment status
    [HttpGet("payments/{paymentId}/status")]

    public async Task<IActionResult> GetPaymentStatus(int paymentId, [FromQuery] int restaurantId)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.PaymentID == paymentId && p.RestaurantID == restaurantId);

            if (payment == null)
                return NotFound(new { message = "Payment not found." });

            return Ok(new
            {
                paymentId = payment.PaymentID,
                orderId = payment.OrderID,
                status = payment.PaymentStatus.ToString(),
                method = payment.PaymentMethod,
                amount = payment.Amount,
                createdAt = payment.CreatedAt,
                completedAt = payment.CompletedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error checking payment status: {ex.Message}");
            return StatusCode(500, new { message = "Error checking payment status." });
        }
    }

    // Fix the cash completion endpoint - make sure it has proper authorization
    [HttpPost("payments/{paymentId}/cash-complete")]
    public async Task<IActionResult> CompleteCashPayment(int paymentId, [FromQuery] int restaurantId)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.PaymentID == paymentId && p.RestaurantID == restaurantId);

            if (payment == null)
                return NotFound(new { message = "Payment not found for this restaurant." });

            payment.PaymentStatus = PaymentStatus.Success;
            payment.CompletedAt = DateTime.UtcNow;
            payment.PaymentMethod = "Cash";

            if (payment.Order != null)
            {
                payment.Order.OrderStatus = OrderStatus.Completed;
                payment.Order.ClosedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Cash payment completed successfully!",
                paymentId = payment.PaymentID,
                orderId = payment.OrderID
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error completing cash payment: {ex.Message}");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while completing cash payment.",
                error = ex.Message
            });
        }
    }


    [HttpPut("payments/{paymentId}/complete")]
    public async Task<IActionResult> CompletePayment(int paymentId, [FromQuery] int restaurantId)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.PaymentID == paymentId && p.RestaurantID == restaurantId);

            if (payment == null)
                return NotFound(new { message = "Payment not found for this restaurant." });

            // Update payment status
            payment.PaymentStatus = PaymentStatus.Success;
            payment.CompletedAt = DateTime.UtcNow;

            // Move order to history (Completed status)
            if (payment.Order != null)
            {
                payment.Order.OrderStatus = OrderStatus.Completed;
                payment.Order.ClosedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Payment completed successfully! Order moved to history.",
                paymentId = payment.PaymentID,
                orderId = payment.OrderID
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error completing payment: {ex.Message}");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while completing payment."
            });
        }


    }
    [HttpGet("report/comprehensive-sales")]
    public async Task<IActionResult> GetComprehensiveSalesReport(
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    [FromQuery] int restaurantId)
    {
        try
        {
            endDate ??= DateTime.UtcNow;
            startDate ??= endDate.Value.AddDays(-30);

            var orders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .Where(o => o.RestaurantID == restaurantId &&
                           o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .ToListAsync();

            var restaurant = await _context.Restaurants.FindAsync(restaurantId);

            var result = new
            {
                RestaurantInfo = new
                {
                    restaurant?.Name,
                    restaurant?.Description,
                    UPI_ID = restaurant?.UPI_ID,
                    UPI_Name = restaurant?.UPI_Name
                    // Removed Address and Phone since they don't exist in the model
                },
                ReportPeriod = new
                {
                    StartDate = startDate.Value.ToString("yyyy-MM-dd"),
                    EndDate = endDate.Value.ToString("yyyy-MM-dd"),
                    Days = (endDate.Value - startDate.Value).Days + 1
                },
                Summary = new
                {
                    TotalOrders = orders.Count,
                    CompletedOrders = orders.Count(o => o.OrderStatus == OrderStatus.Completed),
                    CancelledOrders = orders.Count(o => o.OrderStatus == OrderStatus.Cancelled),
                    TotalRevenue = orders.Where(o => o.OrderStatus == OrderStatus.Completed).Sum(o => o.TotalAmount),
                    AverageOrderValue = orders.Any(o => o.OrderStatus == OrderStatus.Completed) ?
                        orders.Where(o => o.OrderStatus == OrderStatus.Completed).Average(o => o.TotalAmount) : 0,
                    SuccessRate = orders.Any() ? (decimal)orders.Count(o => o.OrderStatus == OrderStatus.Completed) / orders.Count : 0
                },
                DailyBreakdown = orders
                    .GroupBy(o => o.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        Orders = g.Count(),
                        Revenue = g.Where(o => o.OrderStatus == OrderStatus.Completed).Sum(o => o.TotalAmount),
                        AverageOrderValue = g.Where(o => o.OrderStatus == OrderStatus.Completed).Any() ?
                            g.Where(o => o.OrderStatus == OrderStatus.Completed).Average(o => o.TotalAmount) : 0
                    })
                    .OrderBy(x => x.Date)
                    .ToList(),
                PaymentMethodAnalysis = orders
                    .Where(o => o.Payments.Any())
                    .GroupBy(o => o.Payments.OrderByDescending(p => p.CreatedAt).First().PaymentMethod)
                    .Select(g => new
                    {
                        Method = g.Key,
                        Count = g.Count(),
                        Amount = g.Sum(o => o.TotalAmount),
                        Percentage = orders.Where(o => o.Payments.Any()).Sum(o => o.TotalAmount) > 0 ?
                            (g.Sum(o => o.TotalAmount) / orders.Where(o => o.Payments.Any()).Sum(o => o.TotalAmount)) * 100 : 0
                    })
                    .ToList(),
                TopSellingItems = orders
                    .SelectMany(o => o.OrderItems)
                    .Where(oi => oi.Product != null)
                    .GroupBy(oi => oi.Product.ProductName)
                    .Select(g => new
                    {
                        Item = g.Key,
                        Quantity = g.Sum(x => x.Quantity),
                        Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(10)
                    .ToList()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating comprehensive sales report: {ex.Message}");
            return StatusCode(500, "An error occurred while generating the comprehensive report.");
        }
    }
    // In: OrderController.cs

    [HttpPut("{orderId}/change-table")]
    public async Task<IActionResult> ChangeOrderTable(int orderId, [FromQuery] int restaurantId, [FromBody] JsonElement payload)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Validate the incoming request payload
            if (!payload.TryGetProperty("newTableNo", out var tableProp) ||
                !payload.TryGetProperty("changedByUserId", out var changedByProp))
            {
                return BadRequest("newTableNo and changedByUserId are required.");
            }

            int newTableNo = tableProp.GetInt32();
            int changedByUserId = changedByProp.GetInt32();

            // 2. Find the order to be updated
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);

            if (order == null)
            {
                return NotFound(new { message = "Order not found." });
            }

            int oldTableNo = order.RestaurantTableID ?? 0;
            if (oldTableNo == newTableNo)
            {
                // If the table is not actually changing, no action is needed.
                return Ok(new { message = "Table number is already set to the requested value." });
            }

            // 3. Confirm the new table is valid for this restaurant
            var tableExists = await _context.RestaurantTables
                .AnyAsync(t => t.RestaurantTableID == newTableNo && t.RestaurantID == restaurantId);

            if (!tableExists)
            {
                return BadRequest(new { message = $"Table number {newTableNo} is not valid for this restaurant." });
            }

            // 4. Update the order's table number
            order.RestaurantTableID = newTableNo;

            // 5. IMPORTANT: Also update the table number on any PENDING payments for this order
            var pendingPayments = await _context.Payments
                .Where(p => p.OrderID == orderId && p.PaymentStatus == PaymentStatus.Pending)
                .ToListAsync();

            foreach (var payment in pendingPayments)
            {
                payment.TableNo = newTableNo;
            }

            // 6. Log this action for auditing purposes
            string description = $"Table changed from {oldTableNo} to {newTableNo}";
            await LogOrderChange(orderId, "TABLE_CHANGED", description, changedByUserId, restaurantId);

            // 7. Save all changes and commit the transaction
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "Order table changed successfully." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError($"Error changing order table for OrderID {orderId}: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred while changing the table." });
        }
    }
    [HttpGet("with-waiter")]
    public async Task<IActionResult> GetOrdersWithWaiters([FromQuery] int restaurantId) // ✅ Add restaurantId parameter
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Customizations)
                .ThenInclude(c => c.CustomizationOption)
            .Include(o => o.Payments.OrderByDescending(p => p.CreatedAt))
            .Where(o => o.RestaurantID == restaurantId) // ✅ CRITICAL: Add this filter
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
                subtotal = order.Subtotal,
                discountAmount = order.DiscountAmount,
                cgst = order.CGST,
                sgst = order.SGST,
                serviceCharge = order.ServiceCharge,
                totalAmount = order.TotalAmount,
                items = order.OrderItems.Select(item => new
                {
                    orderItemID = item.OrderItemID, // ✅ ADD THIS LINE

                    productID = item.ProductID,
                    productName = item.Product?.ProductName ?? $"Product {item.ProductID}",
                    quantity = item.Quantity,
                    unitPrice = item.UnitPrice,
                    customizations = item.Customizations.Select(c => new
                    {
                        c.CustomizationOptionID,
                        optionName = c.CustomizationOption?.Name
                    }).ToList()
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


