using DinkToPdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Models;
using Restaurant_System.Models;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;


[ApiController]
[Route("api/order")]
public class OrderController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;

    private readonly ILogger<OrderController> _logger;
    public OrderController(ApplicationDbContext context, IOrderRepository orderRepository, IProductRepository productRepository, IUserRepository userRepository, IInventoryRepository inventoryRepository, ILogger<OrderController> logger)
    {
        _context = context;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _inventoryRepository = inventoryRepository;

        _logger = logger;

    }
    private async Task<int> GetNextOrderNumberAsync(int restaurantId)
    {
        var lastOrder = await _context.Orders
            .Where(o => o.RestaurantID == restaurantId)
            .OrderByDescending(o => o.OrderNumber)
            .FirstOrDefaultAsync();

        return (lastOrder?.OrderNumber ?? 0) + 1;
    }

    private async Task<decimal> CalculateUnitPriceAsync(
    int productId,
    List<int>? customizationOptionIds,
    int restaurantId)
    {
        var basePrice = await _context.Products
            .Where(p => p.ProductID == productId && p.RestaurantID == restaurantId)
            .Select(p => p.Price)
            .FirstOrDefaultAsync();

        if (basePrice <= 0)
            throw new Exception($"Invalid price for ProductID {productId}");

        decimal customizationTotal = 0;

        if (customizationOptionIds != null && customizationOptionIds.Any())
        {
            customizationTotal = await _context.CustomizationOptions
                .Where(c =>
                    customizationOptionIds.Contains(c.CustomizationOptionID) &&
                    c.RestaurantID == restaurantId)
                .SumAsync(c => c.FixedPrice);
        }

        return basePrice + customizationTotal;
    }


    //[HttpPost("generate")]
    //public async Task<ActionResult> GenerateOrder(
    //    [FromQuery] int restaurantId,
    //    [FromQuery] int? tableNo = null,
    //    [FromQuery] string source = "QR",
    //    [FromQuery] string paymentPreference = "PayLater",
    //    [FromBody] Order? orderData = null)
    //{
    //    if (restaurantId <= 0)
    //        return BadRequest("restaurantId is required");

    //    if (!await _context.Restaurants.AnyAsync(r => r.RestaurantID == restaurantId))
    //        return BadRequest("Unknown restaurantId");

    //    if (orderData == null)
    //        orderData = new Order();

    //    if (orderData.UserID <= 0)
    //    {
    //        var anon = new User
    //        {
    //            UserRole = "customer",
    //            UserName = "Guest",
    //            CreatedAt = DateTime.UtcNow,
    //            UpdatedAt = DateTime.UtcNow,
    //            CreatedBy = "System",
    //            UpdatedBy = "System",
    //            IsAvailable = true,
    //            RestaurantID = restaurantId
    //        };
    //        _context.Users.Add(anon);
    //        await _context.SaveChangesAsync();
    //        orderData.UserID = anon.UserID;
    //    }

    //    int? restaurantTableId = null;
    //    if (tableNo.HasValue)
    //    {
    //        var table = await _context.RestaurantTables
    //            .FirstOrDefaultAsync(t => t.RestaurantTableID == tableNo && t.RestaurantID == restaurantId);

    //        if (table == null)
    //            return BadRequest("Table does not belong to this restaurant");

    //        restaurantTableId = table.RestaurantTableID;
    //    }

    //    var order = new Order
    //    {
    //        UserID = orderData.UserID,
    //        RestaurantID = restaurantId,
    //        RestaurantTableID = restaurantTableId,
    //        CreatedAt = DateTime.UtcNow,
    //        UpdatedAt = DateTime.UtcNow,
    //        CreatedBy = orderData.UserID.ToString(),
    //        UpdatedBy = orderData.UserID.ToString(),
    //        OrderStatus = OrderStatus.Pending,
    //        KitchenStatus = KitchenStatus.Pending,
    //        Source = source.Equals("waiter", StringComparison.OrdinalIgnoreCase)
    //            ? OrderSource.Waiter
    //            : OrderSource.QR,
    //        OrderNumber = await GetNextOrderNumberAsync(restaurantId),
    //        OrderItems = new List<OrderItem>()
    //    };

    //    if (orderData.OrderItems != null)
    //    {
    //        foreach (var inc in orderData.OrderItems)
    //        {
    //            var unitPrice = await CalculateUnitPriceAsync(
    //                inc.ProductID,
    //                inc.CustomizationOptionIds,
    //                restaurantId);

    //            order.OrderItems.Add(new OrderItem
    //            {
    //                ProductID = inc.ProductID,
    //                Quantity = inc.Quantity,
    //                UnitPrice = unitPrice,
    //                BatchID = 1,
    //                RestaurantID = restaurantId,
    //                IsPrepared = false,
    //                AddedToKitchenAt = DateTime.UtcNow,
    //                Customizations = inc.CustomizationOptionIds?
    //                    .Select(id => new OrderItemCustomization
    //                    {
    //                        CustomizationOptionID = id,
    //                        RestaurantID = restaurantId
    //                    }).ToList() ?? new()
    //            });
    //        }
    //    }

    //    _orderRepository.CalculateOrderAmounts(order);

    //    if (!order.OfferLocked && order.AppliedOfferID == null)
    //    {
    //        await _orderRepository.ApplyBestAvailableOfferAsync(order);
    //        _orderRepository.CalculateOrderAmounts(order);
    //    }

    //    await _orderRepository.AddOrderAsync(order);


    //    return Ok(new
    //    {
    //        message = "Order created successfully",
    //        orderID = order.OrderID,
    //        orderNumber = order.OrderNumber,
    //        totalAmount = order.TotalAmount
    //    });
    //}
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

        if (orderData == null)
            orderData = new Order();

        // Parse source properly
        OrderSource parsedSource = source.ToLower() switch
        {
            "waiter" => OrderSource.Waiter,
            "takeaway" => OrderSource.Takeaway,
            _ => OrderSource.QR
        };

        // Create anonymous user if not provided
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

        // Table required only if NOT takeaway
        if (parsedSource != OrderSource.Takeaway && tableNo.HasValue)
        {
            var table = await _context.RestaurantTables
                .FirstOrDefaultAsync(t =>
                    t.RestaurantTableID == tableNo &&
                    t.RestaurantID == restaurantId);

            if (table == null)
                return BadRequest("Table does not belong to this restaurant");

            restaurantTableId = table.RestaurantTableID;
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
            Source = parsedSource,
            OrderNumber = await GetNextOrderNumberAsync(restaurantId),
            OrderItems = new List<OrderItem>()
        };

        if (orderData.OrderItems != null)
        {
            foreach (var inc in orderData.OrderItems)
            {
                var unitPrice = await CalculateUnitPriceAsync(
                    inc.ProductID,
                    inc.CustomizationOptionIds,
                    restaurantId);

                order.OrderItems.Add(new OrderItem
                {
                    ProductID = inc.ProductID,
                    Quantity = inc.Quantity,
                    UnitPrice = unitPrice,
                    BatchID = 1,
                    RestaurantID = restaurantId,
                    IsPrepared = false,
                    AddedToKitchenAt = DateTime.UtcNow,
                    Customizations = inc.CustomizationOptionIds?
                        .Select(id => new OrderItemCustomization
                        {
                            CustomizationOptionID = id,
                            RestaurantID = restaurantId
                        }).ToList() ?? new()
                });
            }
        }

        _orderRepository.CalculateOrderAmounts(order);

        //if (!order.OfferLocked && order.AppliedOfferID == null)
        //{
        //    await _orderRepository.ApplyBestAvailableOfferAsync(order);
        //    _orderRepository.CalculateOrderAmounts(order);
        //}

        await _orderRepository.AddOrderAsync(order);

        return Ok(new
        {
            message = "Order created successfully",
            orderID = order.OrderID,
            orderNumber = order.OrderNumber,
            orderType = order.Source.ToString(),
            totalAmount = order.TotalAmount
        });
    }


    //[HttpPost("{orderId}/addItem")]
    //public async Task<IActionResult> AddItemsToCart(
    //  int orderId,
    //  [FromQuery] int restaurantId,
    //  [FromBody] List<OrderItem> orderItems)
    //{
    //    await using var transaction = await _context.Database.BeginTransactionAsync();

    //    try
    //    {
    //        var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);

    //        if (order == null)
    //            return NotFound("Order not found");

    //        int newBatchId = order.OrderItems.Any()
    //            ? order.OrderItems.Max(i => i.BatchID) + 1
    //            : 1;

    //        foreach (var inc in orderItems)
    //        {
    //            var unitPrice = await CalculateUnitPriceAsync(
    //                inc.ProductID,
    //                inc.CustomizationOptionIds,
    //                restaurantId);

    //            order.OrderItems.Add(new OrderItem
    //            {
    //                ProductID = inc.ProductID,
    //                Quantity = inc.Quantity,
    //                UnitPrice = unitPrice,
    //                BatchID = newBatchId,
    //                RestaurantID = restaurantId,
    //                IsPrepared = false,
    //                AddedToKitchenAt = DateTime.UtcNow
    //            });
    //        }

    //        order.KitchenStatus = KitchenStatus.Pending;

    //        _orderRepository.CalculateOrderAmounts(order);

    //        await _context.SaveChangesAsync();

    //        // 🔥 INVENTORY FIX
    //        if (order.OrderStatus == OrderStatus.Confirmed)
    //        {
    //            var tempOrder = new Order
    //            {
    //                OrderID = order.OrderID,
    //                OrderNumber = order.OrderNumber,
    //                RestaurantID = order.RestaurantID,
    //                OrderItems = orderItems.Select(i => new OrderItem
    //                {
    //                    ProductID = i.ProductID,
    //                    Quantity = i.Quantity
    //                }).ToList()
    //            };

    //            await _inventoryRepository.DeductInventoryForOrderAsync(
    //                tempOrder,
    //                $"ORDER-{order.OrderNumber}-ADD",
    //                order.UpdatedBy ?? "System"
    //            );
    //        }

    //        await transaction.CommitAsync();

    //        return Ok(new
    //        {
    //            message = "Items added successfully",
    //            totalAmount = order.TotalAmount
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        await transaction.RollbackAsync();

    //        _logger.LogError(ex, "AddItemsToCart failed");

    //        return StatusCode(500, "Failed to add items");
    //    }
    //}

    [HttpPost("{orderId}/addItem")]
    public async Task<IActionResult> AddItemsToCart(
    int orderId,
    [FromQuery] int restaurantId,
    [FromBody] List<OrderItem> orderItems)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);

            if (order == null)
                return NotFound("Order not found");

            int newBatchId = order.OrderItems.Any()
                ? order.OrderItems.Max(i => i.BatchID) + 1
                : 1;

            var newItems = new List<OrderItem>();

            foreach (var inc in orderItems)
            {
                var unitPrice = await CalculateUnitPriceAsync(
                    inc.ProductID,
                    inc.CustomizationOptionIds,
                    restaurantId);

                var newItem = new OrderItem
                {
                    ProductID = inc.ProductID,
                    Quantity = inc.Quantity,
                    UnitPrice = unitPrice,
                    BatchID = newBatchId,
                    RestaurantID = restaurantId,
                    IsPrepared = false,
                    AddedToKitchenAt = DateTime.UtcNow
                };

                order.OrderItems.Add(newItem);
                newItems.Add(newItem);
            }

            order.KitchenStatus = KitchenStatus.Pending;

            _orderRepository.CalculateOrderAmounts(order);

            await _context.SaveChangesAsync();

            // 🔥 INVENTORY (only if already confirmed)
            if (order.OrderStatus == OrderStatus.Confirmed)
            {
                var tempOrder = new Order
                {
                    OrderID = order.OrderID,
                    OrderNumber = order.OrderNumber,
                    RestaurantID = order.RestaurantID,
                    OrderItems = orderItems.Select(i => new OrderItem
                    {
                        ProductID = i.ProductID,
                        Quantity = i.Quantity
                    }).ToList()
                };

                await _inventoryRepository.DeductInventoryForOrderAsync(
                    tempOrder,
                    $"ORDER-{order.OrderNumber}-ADD",
                    order.UpdatedBy ?? "System"
                );
            }

            await transaction.CommitAsync();

            // ================== 🔥🔥 KOT PRINT FIX START ==================
            if (order.OrderStatus == OrderStatus.Confirmed)
            {
                var printer = await GetPrinterConfig(restaurantId, "KOT");

                if (printer != null)
                {
                    var payload = new
                    {
                        Type = "KOT",
                        PrinterName = printer.PrinterName,
                        RestaurantName = printer.HeaderText,
                        RestaurantAddress = printer.Address,
                        Footer = printer.FooterText ?? "",

                        Order = new
                        {
                            OrderNumber = order.OrderNumber.ToString(),
                            TableNo = order.RestaurantTableID?.ToString() ?? "0",
                            BatchID = newBatchId, // 🔥 IMPORTANT

                            Items = newItems.Select(i => new
                            {
                                Name = _context.Products
                                    .Where(p => p.ProductID == i.ProductID)
                                    .Select(p => p.ProductName)
                                    .FirstOrDefault() ?? "Item",

                                Qty = i.Quantity,

                                Modifiers = _context.OrderItemCustomizations
                                    .Where(c => c.OrderItemID == i.OrderItemID)
                                    .Select(c => c.CustomizationOption.Name)
                                    .Where(x => x != null)
                                    .ToList()
                            }).ToList()
                        }
                    };

                    await SavePrintJob(restaurantId, payload);
                }
            }
            // ================== 🔥🔥 KOT PRINT FIX END ==================

            return Ok(new
            {
                message = "Items added successfully",
                totalAmount = order.TotalAmount
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(ex, "AddItemsToCart failed");

            return StatusCode(500, "Failed to add items");
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
                orderNumber = order.OrderNumber,
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
                orderID = order.OrderID,
                orderNumber = order.OrderNumber,
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
        var order = await _orderRepository.GetOrderByIdWithItemsAsync(id, restaurantId);
        if (order == null)
            return NotFound();

        _orderRepository.CalculateOrderAmounts(order);
        //if (!order.OfferLocked && order.AppliedOfferID == null)
        //{
        //    await _orderRepository.ApplyBestAvailableOfferAsync(order);
        //}

        return Ok(new
        {
            orderID = order.OrderID,
            orderNumber = order.OrderNumber,
            orderStatus = order.OrderStatus.ToString(),
            orderItems = order.OrderItems.Select(i => new
            {
                i.ProductID,
                productName = i.Product?.ProductName,
                i.Quantity,
                unitPrice = i.UnitPrice,
                lineTotal = i.UnitPrice * i.Quantity,
                customizations = i.Customizations.Select(c => new
                {
                    c.CustomizationOptionID,
                    c.CustomizationOption.Name,
                    c.CustomizationOption.FixedPrice
                })
            }),
            subtotal = order.Subtotal,
            totalAmount = order.TotalAmount
        });
    }


    [HttpGet("with-waiter/{waiterUserId}")]
    public IActionResult GetOrdersByWaiter(int waiterUserId)
    {
        var orders = _context.Orders
            .Where(o => o.WaiterUserID == waiterUserId)
            .ToList();

        return Ok(orders);
    }

    //[HttpPost("{orderId}/confirm")]
    //public async Task<IActionResult> ConfirmOrder(
    //    int orderId,
    //    [FromQuery] int restaurantId)
    //{
    //    if (restaurantId <= 0)
    //        return BadRequest("restaurantId is required.");

    //    // Validate restaurant exists
    //    var restaurantExists = await _context.Restaurants
    //        .AnyAsync(r => r.RestaurantID == restaurantId);

    //    if (!restaurantExists)
    //        return BadRequest("Invalid restaurantId.");

    //    var order = await _orderRepository
    //        .GetOrderByIdWithItemsAsync(orderId, restaurantId);

    //    if (order == null)
    //        return NotFound("Order not found.");

    //    if (order.OrderStatus != OrderStatus.Pending)
    //        return BadRequest("Order already processed.");

    //    await using var tx = await _context.Database.BeginTransactionAsync();

    //    try
    //    {
    //        // 🔹 STEP 1: Recalculate totals (very important)
    //        _orderRepository.CalculateOrderAmounts(order);

    //        //if (order.AppliedOfferID == null || order.AppliedOfferID == 0)
    //        //{
    //        //    await _orderRepository.ApplyBestAvailableOfferAsync(order);
    //        //    _orderRepository.CalculateOrderAmounts(order);
    //        //}
    //        _orderRepository.CalculateOrderAmounts(order);
    //        order.OfferLocked = true;

    //        order.OfferLocked = true;


    //        // 🔹 STEP 3: Deduct inventory
    //        await _inventoryRepository.DeductInventoryForOrderAsync(
    //            order,
    //            $"ORDER-{order.OrderNumber}",
    //            order.UpdatedBy ?? "System"
    //        );

    //        // 🔹 STEP 4: Update order status
    //        order.OrderStatus = OrderStatus.Confirmed;
    //        order.KitchenStatus = KitchenStatus.Pending;
    //        order.UpdatedAt = DateTime.UtcNow;
    //        order.UpdatedBy ??= "System";

    //        await _context.SaveChangesAsync();

    //        await tx.CommitAsync();

    //        // 🔹 STEP 5: Send KOT print AFTER commit
    //        var printer = await GetPrinterConfig(restaurantId, "KOT");

    //        if (printer != null)
    //        {
    //            var payload = new
    //            {
    //                Type = "KOT",
    //                PrinterName = printer.PrinterName,
    //                RestaurantName = printer.HeaderText,
    //                RestaurantAddress = printer.Address,
    //                Footer = printer.FooterText ?? "",

    //                Order = new
    //                {
    //                    OrderNumber = order.OrderNumber.ToString(),
    //                    TableNo = order.RestaurantTableID?.ToString() ?? "0",
    //                    Items = order.OrderItems.Select(i => new
    //                    {
    //                        Name = i.Product?.ProductName ?? "Item",
    //                        Qty = i.Quantity,
    //                        Modifiers = i.Customizations
    //                            .Select(c => c.CustomizationOption?.Name)
    //                            .Where(x => !string.IsNullOrEmpty(x))
    //                            .ToList()
    //                    }).ToList()
    //                }
    //            };

    //            await SavePrintJob(restaurantId, payload);
    //        }

    //        return Ok(new
    //        {
    //            message = "Order confirmed successfully",
    //            orderID = order.OrderID,
    //            orderNumber = order.OrderNumber,
    //            totalAmount = order.TotalAmount,
    //            status = order.OrderStatus.ToString()
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        await tx.RollbackAsync();

    //        _logger.LogError(ex,
    //            $"ConfirmOrder failed for OrderID {orderId}, RestaurantID {restaurantId}");

    //        return StatusCode(500, "Failed to confirm order.");
    //    }
    //}

    [HttpPost("{orderId}/confirm")]
    public async Task<IActionResult> ConfirmOrder(int orderId, [FromQuery] int restaurantId)
    {
        _logger.LogInformation($"🚀 ConfirmOrder START | OrderID={orderId}");

        if (restaurantId <= 0) return BadRequest("restaurantId is required.");

        // 1. Fetch Order - Optimization: Use Split Query behavior if your repo allows
        var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);

        if (order == null) return NotFound("Order not found.");
        if (order.OrderStatus != OrderStatus.Pending) return BadRequest("Order already processed.");

        // 2. Transaction - Keep it as short as possible
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            _orderRepository.CalculateOrderAmounts(order);
            order.OfferLocked = true;

            // This method is likely where the 600ms delay is happening. 
            // Ensure ProductID is indexed in your DB.
            await _inventoryRepository.DeductInventoryForOrderAsync(
                order, $"ORDER-{order.OrderNumber}", order.UpdatedBy ?? "System"
            );

            order.OrderStatus = OrderStatus.Confirmed;
            order.KitchenStatus = KitchenStatus.Pending;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            _logger.LogInformation("✅ Order Transaction Committed");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "🔥 ConfirmOrder Transaction FAILED");
            return StatusCode(500, "Database error during confirmation.");
        }

        // 3. Printing - Run outside the transaction so failures don't roll back the order
        var printer = await GetPrinterConfig(restaurantId, "KOT");
        bool printQueued = false;

        if (printer != null)
        {
            try
            {
                var payload = new
                {
                    Type = "KOT",
                    PrinterName = printer.PrinterName,
                    RestaurantName = printer.HeaderText,
                    Order = new
                    {
                        OrderNumber = order.OrderNumber.ToString(),
                        TableNo = order.RestaurantTableID?.ToString() ?? "0",
                        Items = order.OrderItems.Select(i => new { Name = i.Product?.ProductName ?? "Item", Qty = i.Quantity }).ToList()
                    }
                };
                await SavePrintJob(restaurantId, payload);
                printQueued = true;
            }
            catch (Exception ex) { _logger.LogError(ex, "KOT Print Job failed to save."); }
        }
        else
        {
            _logger.LogWarning("❌ KOT Printer config not found. Order confirmed but not printed.");
        }

        return Ok(new
        {
            message = printQueued ? "Order confirmed and KOT sent." : "Order confirmed (Printer not configured).",
            orderID = order.OrderID,
            printStatus = printQueued ? "Success" : "PrinterNotFound"
        });
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
                oi.Order.RestaurantID == restaurantId &&
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
                    orderNumber = order.OrderNumber,
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
            orderNumber = o.OrderNumber,
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

                int tableNo = order.RestaurantTableID.HasValue ? order.RestaurantTableID.Value : 0;

                _context.WaiterNotifications.Add(new WaiterNotification
                {
                    OrderId = orderId,
                    TableNo = tableNo,
                    Message = $"Order #{order.OrderNumber} for Table {tableNo} is ready",
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

        var latestBatchId = order.OrderItems.Max(oi => oi.BatchID);

        foreach (var item in order.OrderItems.Where(oi => oi.BatchID == latestBatchId))
        {
            item.IsPrepared = true;
            item.PreparedAt = now;
        }

        order.LastKitchenReadyAt = now;
        order.KitchenStatus = KitchenStatus.Ready;
        order.OrderStatus = OrderStatus.Confirmed;
        order.UpdatedAt = now;

        string label = order.Source == OrderSource.Takeaway
            ? "Takeaway"
            : $"Table {order.RestaurantTableID ?? 0}";

        var notification = new WaiterNotification
        {
            OrderId = orderId,
            TableNo = order.RestaurantTableID ?? 0,
            Message = $"Order #{order.OrderNumber} ({label}) is ready",
            IsAcknowledged = false,
            CreatedAt = now,
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

    [HttpPut("kitchen/update-status/{orderId}")]
    public async Task<IActionResult> UpdateKitchenStatus(int orderId, [FromBody] string status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return NotFound();

        if (status.Equals("Ready", StringComparison.OrdinalIgnoreCase))
        {
            order.KitchenStatus = KitchenStatus.Ready;
            order.OrderStatus = OrderStatus.Confirmed;
            int tableNo = order.RestaurantTableID.HasValue ? order.RestaurantTableID.Value : 0;
            var notification = new WaiterNotification
            {
                OrderId = orderId,
                TableNo = tableNo,
                Message = $"Order #{order.OrderNumber} for Table {tableNo} is ready",
                CreatedAt = DateTime.UtcNow,
                IsAcknowledged = false,
                RestaurantID = order.RestaurantID
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
    public async Task<IActionResult> ServeOrder(
        int orderId,
        [FromQuery] int restaurantId)
    {
        var order = await _context.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o =>
                o.OrderID == orderId &&
                o.RestaurantID == restaurantId);

        if (order == null)
            return NotFound(new { message = "Order not found." });

        if (order.OrderStatus == OrderStatus.Completed)
            return BadRequest(new { message = "Order already completed." });

        order.OrderStatus = OrderStatus.Served;

        // 🔥 Check if already fully paid
        var totalPaid = order.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Success)
            .Sum(p => p.Amount);

        if (totalPaid >= order.TotalAmount)
        {
            order.OrderStatus = OrderStatus.Completed;
            order.ClosedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Order marked as served.",
            orderStatus = order.OrderStatus
        });
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
                orderNumber = order.OrderNumber,
                orderStatus = order.OrderStatus
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error completing order: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "An error occurred while completing the order.");
        }
    }

    //[HttpPut("{orderId}/cancel")]
    //public async Task<IActionResult> CancelOrder(int orderId, [FromQuery] int restaurantId)
    //{
    //    try
    //    {
    //        var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);
    //        if (order == null || order.RestaurantID != restaurantId)
    //            return NotFound(new { message = "Order not found for this restaurant." });

    //        order.ClosedAt = DateTime.UtcNow;
    //        order.OrderStatus = OrderStatus.Cancelled;

    //        await _orderRepository.UpdateOrderAsync(order);

    //        return Ok(new
    //        {
    //            message = "Order cancelled successfully!",
    //            orderID = order.OrderID,
    //            orderNumber = order.OrderNumber,
    //            orderStatus = order.OrderStatus
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError($"Error cancelling order: {ex.Message}\n{ex.StackTrace}");
    //        return StatusCode(500, "An error occurred while cancelling the order.");
    //    }
    //}

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
    private async Task RecalculatePaymentStatusAsync(Order order)
    {
        var successfulPayments = await _context.Payments
            .Where(p => p.OrderID == order.OrderID &&
                        p.PaymentStatus == PaymentStatus.Success)
            .ToListAsync();

        order.PaidAmount = successfulPayments.Sum(p => p.Amount);
        order.RemainingAmount = order.TotalAmount - order.PaidAmount;

        if (order.RemainingAmount <= 0)
        {
            order.RemainingAmount = 0;
            order.OrderStatus = OrderStatus.Completed;
            order.ClosedAt = DateTime.UtcNow;
        }
        else
        {
            // Paid partially or not paid at all
            if (order.OrderStatus == OrderStatus.Served)
                order.OrderStatus = OrderStatus.Served;
        }

        order.UpdatedAt = DateTime.UtcNow;
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

            return Ok(new { message = $"Waiter {waiterId} assigned to Order {order.OrderNumber} successfully!" });
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
            .Where(r => r.RestaurantID == restaurantId)
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


    //[HttpGet("{orderId}/bill")]
    //public async Task<IActionResult> DownloadBill(int orderId)
    //{
    //    var order = await _context.Orders
    //        .Include(o => o.OrderItems)
    //          .ThenInclude(oi => oi.Customizations)
    //            .ThenInclude(c => c.CustomizationOption)
    //            .ThenInclude(oi => oi.Product)
    //        .Include(o => o.RestaurantTable)
    //        .FirstOrDefaultAsync(o => o.OrderID == orderId);

    //    if (order == null)
    //        return NotFound();

    //    var restaurant = await _context.Restaurants.FirstOrDefaultAsync();

    //    _orderRepository.CalculateOrderAmounts(order);
    //    //await _orderRepository.ApplyBestAvailableOfferAsync(order);
    //    await _context.SaveChangesAsync();

    //    var pdfBytes = Document.Create(container =>
    //    {
    //        container.Page(page =>
    //        {
    //            page.Size(PageSizes.A4);
    //            page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);

    //            page.Header().Column(col =>
    //            {
    //                col.Item().AlignCenter().Text(restaurant?.Name ?? "Restaurant Name")
    //                    .Bold().FontSize(22);

    //                if (!string.IsNullOrEmpty(restaurant?.Description))
    //                    col.Item().AlignCenter().Text(restaurant.Description).FontSize(12).Italic();

    //                var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
    //                    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

    //                col.Item().AlignCenter().Text($"Date: {istTime:dd MMM yyyy | hh:mm tt}")
    //                    .FontSize(10).FontColor(Colors.Grey.Darken2);

    //                col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
    //            });

    //            page.Content().Column(column =>
    //            {
    //                column.Item().PaddingBottom(10).Text($"Order Number: #{order.OrderNumber}")
    //                    .Bold().FontSize(14);

    //                column.Item().Table(table =>
    //                {
    //                    table.ColumnsDefinition(columns =>
    //                    {
    //                        columns.ConstantColumn(10, QuestPDF.Infrastructure.Unit.Millimetre);
    //                        columns.RelativeColumn(3);
    //                        columns.RelativeColumn();
    //                        columns.RelativeColumn();
    //                        columns.RelativeColumn();
    //                    });

    //                    table.Header(header =>
    //                    {
    //                        header.Cell().Text("#").Bold();
    //                        header.Cell().Text("Item").Bold();
    //                        header.Cell().AlignRight().Text("Qty").Bold();
    //                        header.Cell().AlignRight().Text("Price").Bold();
    //                        header.Cell().AlignRight().Text("Total").Bold();
    //                    });
    //                    foreach (var (item, index) in order.OrderItems.Select((x, i) => (x, i)))
    //                    {
    //                        table.Cell().Text($"{index + 1}");
    //                        table.Cell().Text(item.Product?.ProductName ?? "Unknown");

    //                        if (item.Customizations.Any())
    //                        {
    //                            var customizationNames = string.Join(", ", item.Customizations.Select(c => c.CustomizationOption.Name));
    //                            table.Cell().Text(text =>
    //                            {
    //                                text.Span(item.Product?.ProductName ?? "Unknown");
    //                                text.EmptyLine();
    //                                text.Span($"Custom: {customizationNames}").FontColor(Colors.Grey.Medium).FontSize(8);
    //                            });
    //                        }
    //                        else
    //                        {
    //                            table.Cell().Text(item.Product?.ProductName ?? "Unknown");
    //                        }

    //                        table.Cell().AlignRight().Text(item.Quantity.ToString());
    //                        table.Cell().AlignRight().Text($"₹{item.UnitPrice:N2}");
    //                        table.Cell().AlignRight().Text($"₹{item.UnitPrice * item.Quantity:N2}");
    //                    }
    //                });

    //                column.Item().PaddingTop(15).AlignRight().Text(text =>
    //                {
    //                    text.Span("Subtotal: ").Bold();
    //                    text.Span($"₹{order.Subtotal:N2}");
    //                    text.EmptyLine();

    //                    if (order.AppliedOffer != null)
    //                    {
    //                        text.Span("Discount (");
    //                        text.Span(order.AppliedOffer.Description ?? "Offer").Italic();
    //                        text.Span("): ").Bold();
    //                        text.Span($"- ₹{order.DiscountAmount:N2}");
    //                        text.EmptyLine();
    //                    }

    //                    text.Span("CGST: ").Bold();
    //                    text.Span($"₹{order.CGST:N2}");
    //                    text.EmptyLine();

    //                    text.Span("SGST: ").Bold();
    //                    text.Span($"₹{order.SGST:N2}");
    //                    text.EmptyLine();

    //                    text.Span("Service Charge: ").Bold();
    //                    text.Span($"₹{order.ServiceCharge:N2}");
    //                    text.EmptyLine();

    //                    text.Span("Total: ").Bold().FontSize(14);
    //                    text.Span($"₹{order.TotalAmount:N2}").FontSize(14);
    //                });
    //            });

    //            page.Footer().Column(col =>
    //            {
    //                col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
    //                col.Item().AlignCenter().Text("Thank you for dining with us!").Bold().FontSize(12);
    //                col.Item().AlignCenter().Text("Visit us again.").FontSize(10).Italic();
    //            });
    //        });
    //    }).GeneratePdf();

    //    return File(pdfBytes, "application/pdf", $"Bill_Order_{order.OrderNumber}.pdf");
    //}

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
        return Ok(new { message = "Order marked as served" });
    }

    [HttpPost("{orderId}/pending")]
    public async Task<IActionResult> CreatePendingPayment(
      int orderId,
      [FromQuery] int restaurantId,
      [FromBody] JsonElement payload)
    {
        var order = await _context.Orders
            .Include(o => o.Payments)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o =>
                o.OrderID == orderId &&
                o.RestaurantID == restaurantId);

        if (order == null)
            return NotFound(new { message = "Order not found." });

        // 🔥 Always recalc
        _orderRepository.CalculateOrderAmounts(order);
        await _context.SaveChangesAsync();

        // 🔹 Paid so far
        var alreadyPaid = order.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Success)
            .Sum(p => p.Amount);

        var remaining = Math.Max(order.TotalAmount - alreadyPaid, 0);

        if (remaining <= 0)
        {
            return BadRequest(new
            {
                message = "Order already fully paid",
                totalAmount = order.TotalAmount,
                paidAmount = alreadyPaid
            });
        }

        // 🔹 Get method
        string method = payload.TryGetProperty("method", out var m)
            ? m.GetString() ?? "Cash"
            : "Cash";

        // 🔹 Get amount (IMPORTANT FIX)
        decimal amount = payload.TryGetProperty("amount", out var amt)
            ? amt.GetDecimal()
            : remaining;

        // 🔥 VALIDATION
        if (amount <= 0)
            return BadRequest("Invalid payment amount");

        if (amount > remaining)
            return BadRequest($"Amount exceeds remaining ₹{remaining}");

        // 🔥 ALWAYS CREATE NEW PAYMENT (no reuse)
        var payment = new Payment
        {
            OrderID = orderId,
            TableNo = order.RestaurantTableID ?? 0,
            Amount = amount,
            PaymentMethod = method,
            PaymentStatus = PaymentStatus.Pending,
            RestaurantID = restaurantId,
            CreatedAt = DateTime.UtcNow,
            IsPartial = amount < remaining
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            paymentId = payment.PaymentID,
            amount = payment.Amount,
            remainingAmount = remaining,
            isPartial = payment.IsPartial
        });
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
            upiID = restaurant.UPI_ID,
            upiName = restaurant.UPI_Name ?? restaurant.Name
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
            .FirstOrDefaultAsync(p => p.PaymentID == id && p.RestaurantID == restaurantId);

        if (payment == null)
            return NotFound("Payment not found");

        payment.PaymentStatus = PaymentStatus.Success;
        payment.CompletedAt = DateTime.UtcNow;

        // ✅ USE CENTRAL METHOD
        if (payment.Order != null)
        {
            await RecalculatePaymentStatusAsync(payment.Order);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Payment completed successfully" });
    }
    [HttpPost("{orderId}/initiate-payment")]
    public async Task<IActionResult> InitiatePayment(
    int orderId,
    [FromQuery] int restaurantId,
    [FromBody] JsonElement payload,
    [FromQuery] string method = "UPI",
    [FromQuery] string channel = "Customer")
    {
        _logger.LogInformation($"🚀 START initiate-payment | OrderID={orderId}, RestaurantID={restaurantId}");

        try
        {
            if (restaurantId <= 0)
                return BadRequest("restaurantId is required");

            var order = await _context.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o =>
                    o.OrderID == orderId &&
                    o.RestaurantID == restaurantId);

            if (order == null)
                return NotFound("Order not found");

            if (order.TotalAmount <= 0)
                return BadRequest("Invalid order amount");

            await RecalculatePaymentStatusAsync(order);

            if (order.RemainingAmount <= 0)
            {
                return BadRequest(new { message = "Order already fully paid" });
            }

            var existingPending = order.Payments
                .FirstOrDefault(p => p.PaymentStatus == PaymentStatus.Pending);

            if (existingPending != null)
            {
                return Ok(new
                {
                    message = "Payment already initiated",
                    paymentId = existingPending.PaymentID,
                    amount = existingPending.Amount,
                    status = existingPending.PaymentStatus.ToString()
                });
            }

            decimal amountToPay = order.RemainingAmount;

            if (payload.TryGetProperty("amount", out var amtProp))
            {
                var requestedAmount = amtProp.GetDecimal();
                if (requestedAmount > 0 && requestedAmount <= order.RemainingAmount)
                    amountToPay = requestedAmount;
            }

            bool isPartial = amountToPay < order.RemainingAmount;

            var payment = new Payment
            {
                OrderID = orderId,
                RestaurantID = restaurantId,
                Amount = amountToPay,

                // ✅ STRING (NOT ENUM)
                PaymentMethod = method?.ToUpper() ?? "UPI",

                // ✅ ENUM (correct)
                PaymentChannel = Enum.TryParse<PaymentChannel>(channel, true, out var parsedChannel)
          ? parsedChannel
          : PaymentChannel.Customer,

                PaymentStatus = PaymentStatus.Pending,
                IsPartial = isPartial,
                CreatedAt = DateTime.UtcNow,
                IsNotified = false,
                TableNo = order.RestaurantTableID ?? 0
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Payment initiated successfully",
                paymentId = payment.PaymentID,
                orderId = order.OrderID,
                orderNumber = order.OrderNumber,
                amount = payment.Amount,
                isPartial = payment.IsPartial,
                status = payment.PaymentStatus.ToString(),
                method = payment.PaymentMethod.ToString(),
                channel = payment.PaymentChannel.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 initiate-payment FAILED");

            return StatusCode(500, new
            {
                message = "Payment initiation failed",
                error = ex.Message
            });
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
                paymentID = p.PaymentID,
                orderID = p.OrderID,
                tableNo = p.TableNo,
                status = p.PaymentStatus.ToString(),
                method = p.PaymentMethod,
                orderNumber = p.Order?.OrderNumber,
                amount = p.Amount,
                paymentChannel = p.PaymentChannel,
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
            .Where(o => o.RestaurantTable.RestaurantID == restaurantId)
            .ToListAsync();

        return Ok(new
        {
            message = "Orders fetched successfully for franchise.",
            orders = orders.Select(order => new
            {
                orderID = order.OrderID,
                orderNumber = order.OrderNumber,
                createdAt = order.CreatedAt,
                closedAt = order.ClosedAt,
                tableNo = order.RestaurantTableID,
                restaurantId = order.RestaurantTable.RestaurantID,

                orderStatus = Enum.GetName(typeof(OrderStatus), order.OrderStatus),
                items = order.OrderItems.Select(item => new
                {
                    productID = item.ProductID,
                    productName = item.Product?.ProductName ?? $"Product {item.ProductID}",
                    quantity = item.Quantity
                }),
                latestPayment = order.Payments
    .Where(p => p.PaymentStatus == PaymentStatus.Success)
    .OrderByDescending(p => p.CompletedAt ?? p.CreatedAt)
    .Select(p => new
    {
        method = p.PaymentMethod,
        status = p.PaymentStatus.ToString(),
        amount = p.Amount,
        paidAt = p.CompletedAt
    })
    .FirstOrDefault()

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
            orderNumber = order.OrderNumber,
            orderStatus = order.OrderStatus,
            createdAt = order.CreatedAt
        });
    }




    private DateTime EnsureUtc(DateTime dateTime)
    {
        return dateTime.Kind == DateTimeKind.Utc
            ? dateTime
            : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }


    private (DateTime, DateTime) GetDateRange(string timeRange, string? customStart, string? customEnd)
    {
        DateTime today = DateTime.Today.ToUniversalTime();
        DateTime startDate = today;
        DateTime endDate = today.AddDays(1).AddSeconds(-1);

        switch (timeRange.ToLower())
        {
            case "today":
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

            default:
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

//    [HttpGet("{orderId}/bill-html")]
//    public async Task<IActionResult> GetBillHtml(int orderId, [FromQuery] int restaurantId)
//    {
//        var order = await _context.Orders
//            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
//            .Include(o => o.RestaurantTable)
//            .FirstOrDefaultAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);

//        if (order == null)
//            return NotFound();

//        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.RestaurantID == restaurantId);
//        _orderRepository.CalculateOrderAmounts(order);
//        await _orderRepository.ApplyBestAvailableOfferAsync(order);
//        await _context.SaveChangesAsync();

//        var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

//        var html = $@"<!DOCTYPE html>
//<html>
//<head>
//  <meta charset='UTF-8'>
//  <title>Order Bill #{order.OrderID}</title>
//  <style>
//    body {{
//      font-family: 'Segoe UI', sans-serif;
//      padding: 20px;
//      background: #fff;
//    }}
//    .bill-container {{ max-width: 700px; margin: auto; }}
//    .restaurant-header {{ text-align: center; }}
//    .restaurant-header h2 {{ margin: 0; }}
//    table {{ width: 100%; margin-top: 20px; border-collapse: collapse; font-size: 14px; }}
//    th, td {{ border: 1px solid #ccc; padding: 8px; text-align: left; }}
//    th {{ background: #f2f2f2; }}
//    .totals {{ margin-top: 20px; text-align: right; }}
//    .footer {{ text-align: center; margin-top: 30px; font-size: 14px; color: #555; }}
//  </style>
//</head>
//<body>
//<div class='bill-container'>
//  <div class='restaurant-header'>
//    <h2>{restaurant?.Name ?? "Restaurant"}</h2>
//    <p>{restaurant?.Description ?? ""}</p>
//    <p>Date: {istNow:dd MMM yyyy hh:mm tt}</p>
//  </div>
//  <p><strong>Order ID:</strong> #{order.OrderID}</p>
//  <p><strong>Table No:</strong> {order.RestaurantTable?.TableName ?? "N/A"}</p>
//  <table>
//    <thead><tr><th>#</th><th>Item</th><th>Qty</th><th>Rate</th><th>Total</th></tr></thead>
//    <tbody>";

//        int count = 1;
//        foreach (var item in order.OrderItems)
//        {
//            var total = item.Quantity * item.UnitPrice;
//            html += $"<tr><td>{count++}</td><td>{item.Product?.ProductName}</td><td>{item.Quantity}</td><td>₹{item.UnitPrice:N2}</td><td>₹{total:N2}</td></tr>";
//        }

//        html += $@"</tbody></table><div class='totals'>
//    <p>Subtotal: ₹{order.Subtotal:N2}</p>";

//        if (order.AppliedOffer != null)
//            html += $"<p>Discount ({order.AppliedOffer.Description}): -₹{order.DiscountAmount:N2}</p>";

//        html += $@"
//    <p>CGST: ₹{order.CGST:N2}</p>
//    <p>SGST: ₹{order.SGST:N2}</p>
//    <p>Service Charge: ₹{order.ServiceCharge:N2}</p>
//    <p><strong>Grand Total: ₹{order.TotalAmount:N2}</strong></p>
//  </div>
//  <div class='footer'>
//    <p>Thank you for dining with us!</p>
//    <p>Visit again 🙏</p>
//  </div>
//</div>
//</body>
//</html>";

//        return Content(html, "text/html");
//    }


    [HttpPut("{orderId}/update-item/{itemId}")]
    public async Task<IActionResult> UpdateOrderItem(
      int orderId,
      int itemId,
      [FromQuery] int restaurantId,
      [FromBody] JsonElement payload)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);

            if (order == null)
                return NotFound();

            var item = order.OrderItems.FirstOrDefault(i => i.OrderItemID == itemId);

            if (item == null)
                return NotFound();

            if (payload.TryGetProperty("quantity", out var q))
            {
                var oldQty = item.Quantity;
                var newQty = q.GetInt32();

                if (newQty > oldQty && order.OrderStatus == OrderStatus.Confirmed)
                {
                    var diff = newQty - oldQty;

                    var tempOrder = new Order
                    {
                        OrderID = order.OrderID,
                        OrderNumber = order.OrderNumber,
                        RestaurantID = order.RestaurantID,
                        OrderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            ProductID = item.ProductID,
                            Quantity = diff
                        }
                    }
                    };

                    await _inventoryRepository.DeductInventoryForOrderAsync(
                        tempOrder,
                        $"ORDER-{order.OrderNumber}-UPDATE",
                        order.UpdatedBy ?? "System"
                    );
                }

                item.Quantity = newQty;
            }

            _orderRepository.CalculateOrderAmounts(order);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Order item updated",
                newTotal = order.TotalAmount
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(ex, "UpdateOrderItem failed");

            return StatusCode(500, "Failed to update item");
        }
    }

    //[HttpPost("{orderId}/add-item")]
    //public async Task<IActionResult> AddItemToOrder(
    //  int orderId,
    //  [FromQuery] int restaurantId,
    //  [FromBody] JsonElement payload)
    //{
    //    await using var transaction = await _context.Database.BeginTransactionAsync();

    //    try
    //    {
    //        int productId = payload.GetProperty("productID").GetInt32();
    //        int quantity = payload.GetProperty("quantity").GetInt32();

    //        var customizationIds = payload.TryGetProperty("customizationOptionIds", out var cp)
    //            ? cp.EnumerateArray().Select(x => x.GetInt32()).ToList()
    //            : new List<int>();

    //        var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);

    //        if (order == null)
    //            return NotFound();

    //        var unitPrice = await CalculateUnitPriceAsync(
    //            productId,
    //            customizationIds,
    //            restaurantId);

    //        int batchId = order.OrderItems.Any()
    //            ? order.OrderItems.Max(i => i.BatchID) + 1
    //            : 1;

    //        var newItem = new OrderItem
    //        {
    //            ProductID = productId,
    //            Quantity = quantity,
    //            UnitPrice = unitPrice,
    //            BatchID = batchId,
    //            RestaurantID = restaurantId,
    //            IsPrepared = false,
    //            AddedToKitchenAt = DateTime.UtcNow,
    //            Customizations = customizationIds
    //                .Select(id => new OrderItemCustomization
    //                {
    //                    CustomizationOptionID = id,
    //                    RestaurantID = restaurantId
    //                }).ToList()
    //        };

    //        order.OrderItems.Add(newItem);

    //        order.KitchenStatus = KitchenStatus.Pending;

    //        _orderRepository.CalculateOrderAmounts(order);

    //        if (!order.OfferLocked && order.AppliedOfferID == null)
    //            await _orderRepository.ApplyBestAvailableOfferAsync(order);

    //        _orderRepository.CalculateOrderAmounts(order);

    //        await _context.SaveChangesAsync();

    //        // 🔥 INVENTORY FIX
    //        if (order.OrderStatus == OrderStatus.Confirmed)
    //        {
    //            var tempOrder = new Order
    //            {
    //                OrderID = order.OrderID,
    //                OrderNumber = order.OrderNumber,
    //                RestaurantID = order.RestaurantID,
    //                OrderItems = new List<OrderItem>
    //            {
    //                new OrderItem
    //                {
    //                    ProductID = newItem.ProductID,
    //                    Quantity = newItem.Quantity
    //                }
    //            }
    //            };

    //            await _inventoryRepository.DeductInventoryForOrderAsync(
    //                tempOrder,
    //                $"ORDER-{order.OrderNumber}-ADD",
    //                order.UpdatedBy ?? "System"
    //            );
    //        }

    //        await transaction.CommitAsync();

    //        return Ok(new
    //        {
    //            message = "Item added successfully",
    //            newTotal = order.TotalAmount
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        await transaction.RollbackAsync();
    //        _logger.LogError(ex, "AddItemToOrder failed");

    //        return StatusCode(500, "Failed to add item");
    //    }
    //}




    [HttpPost("{orderId}/add-item")]
    public async Task<IActionResult> AddItemToOrder(
    int orderId,
    [FromQuery] int restaurantId,
    [FromBody] JsonElement payload)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1️⃣ Extract payload
            int productId = payload.GetProperty("productID").GetInt32();
            int quantity = payload.GetProperty("quantity").GetInt32();

            var customizationIds = payload.TryGetProperty("customizationOptionIds", out var cp)
                ? cp.EnumerateArray().Select(x => x.GetInt32()).ToList()
                : new List<int>();

            // 2️⃣ Fetch order WITH full navigation properties
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Customizations)
                        .ThenInclude(c => c.CustomizationOption)
                .FirstOrDefaultAsync(o =>
                    o.OrderID == orderId &&
                    o.RestaurantID == restaurantId);

            if (order == null)
                return NotFound(new { message = "Order not found." });

            // 3️⃣ Calculate price including customizations
            var unitPrice = await CalculateUnitPriceAsync(
                productId,
                customizationIds,
                restaurantId);

            // 4️⃣ Create new BatchID
            int batchId = order.OrderItems.Any()
                ? order.OrderItems.Max(i => i.BatchID) + 1
                : 1;

            // 5️⃣ Fetch product (to avoid null names)
            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.ProductID == productId &&
                    p.RestaurantID == restaurantId);

            if (product == null)
                return BadRequest("Invalid product.");

            // 6️⃣ Fetch customization options (for printer)
            var customizationOptions = await _context.CustomizationOptions
                .Where(c =>
                    customizationIds.Contains(c.CustomizationOptionID) &&
                    c.RestaurantID == restaurantId)
                .ToListAsync();

            // 7️⃣ Create new order item
            var newItem = new OrderItem
            {
                ProductID = productId,
                Product = product,
                Quantity = quantity,
                UnitPrice = unitPrice,
                BatchID = batchId,
                RestaurantID = restaurantId,
                IsPrepared = false,
                AddedToKitchenAt = DateTime.UtcNow,
                Customizations = customizationOptions.Select(c => new OrderItemCustomization
                {
                    CustomizationOptionID = c.CustomizationOptionID,
                    CustomizationOption = c,
                    RestaurantID = restaurantId
                }).ToList()
            };

            order.OrderItems.Add(newItem);

            // 8️⃣ Reset kitchen status
            order.KitchenStatus = KitchenStatus.Pending;

            // 9️⃣ Recalculate totals
            _orderRepository.CalculateOrderAmounts(order);

            //if (!order.OfferLocked && order.AppliedOfferID == null)
            //    await _orderRepository.ApplyBestAvailableOfferAsync(order);

            //_orderRepository.CalculateOrderAmounts(order);

            await _context.SaveChangesAsync();

            // 🔟 Deduct inventory if confirmed
            if (order.OrderStatus == OrderStatus.Confirmed)
            {
                var tempOrder = new Order
                {
                    OrderID = order.OrderID,
                    OrderNumber = order.OrderNumber,
                    RestaurantID = order.RestaurantID,
                    OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductID = productId,
                        Quantity = quantity
                    }
                }
                };

                await _inventoryRepository.DeductInventoryForOrderAsync(
                    tempOrder,
                    $"ORDER-{order.OrderNumber}-ADD",
                    order.UpdatedBy ?? "System"
                );
            }

            await transaction.CommitAsync();

            // 🔥 1️⃣1️⃣ PRINT KOT ONLY FOR THIS BATCH
            if (order.OrderStatus == OrderStatus.Confirmed)
            {
                var printer = await GetPrinterConfig(restaurantId, "KOT");

                if (printer != null)
                {
                    var batchItems = order.OrderItems
                        .Where(i => i.BatchID == batchId)
                        .ToList();

                    var payloadToPrint = new
                    {
                        Type = "KOT",
                        PrinterName = printer.PrinterName,
                        RestaurantName = printer.HeaderText,
                        RestaurantAddress = printer.Address,
                        Footer = printer.FooterText ?? "",

                        Order = new
                        {
                            OrderNumber = order.OrderNumber.ToString(),
                            TableNo = order.RestaurantTableID?.ToString() ?? "0",
                            BatchID = batchId,

                            Items = batchItems.Select(i => new
                            {
                                Name = i.Product?.ProductName ?? "Item",
                                Qty = i.Quantity,
                                Modifiers = i.Customizations
                                    .Select(c => c.CustomizationOption?.Name)
                                    .Where(x => !string.IsNullOrEmpty(x))
                                    .ToList()
                            }).ToList()
                        }
                    };

                    await SavePrintJob(restaurantId, payloadToPrint);
                }
            }

            return Ok(new
            {
                message = "Item added successfully",
                newTotal = order.TotalAmount,
                batchId = batchId
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "AddItemToOrder failed");
            return StatusCode(500, "Failed to add item");
        }
    }
    [HttpDelete("{orderId}/cancel")]
    public async Task<IActionResult> CancelOrder(
    int orderId,
    [FromQuery] int restaurantId,
    [FromBody] JsonElement payload)
    {
        try
        {
            var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);

            if (order == null)
                return NotFound();

            // 🔥 INVENTORY RESTORE FIX
            if (order.InventoryProcessed)
            {
                await _inventoryRepository.ReverseInventoryForOrderAsync(order);
            }

            order.OrderStatus = OrderStatus.Cancelled;
            order.ClosedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Order cancelled successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CancelOrder failed");

            return StatusCode(500);
        }
    }
    //[HttpDelete("{orderId}/cancel")]
    //public async Task<IActionResult> CancelOrder(int orderId, [FromQuery] int restaurantId, [FromBody] JsonElement payload)
    //{
    //    try
    //    {
    //        if (!payload.TryGetProperty("changedByUserId", out var changedByProp))
    //            return BadRequest("changedByUserId is required");

    //        int changedByUserId = changedByProp.GetInt32();
    //        string reason = "No reason provided";

    //        if (payload.TryGetProperty("reason", out var reasonProp))
    //        {
    //            reason = reasonProp.GetString();
    //        }

    //        var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId, restaurantId);
    //        if (order == null || order.RestaurantID != restaurantId)
    //            return NotFound(new { message = "Order not found." });

    //        if (order.OrderStatus == OrderStatus.Completed || order.OrderStatus == OrderStatus.Served)
    //            return BadRequest(new { message = "Cannot cancel completed or served orders." });

    //        order.OrderStatus = OrderStatus.Cancelled;
    //        order.ClosedAt = DateTime.UtcNow;
    //        order.KitchenStatus = KitchenStatus.Pending;

    //        var pendingPayments = await _context.Payments
    //            .Where(p => p.OrderID == orderId && p.PaymentStatus == PaymentStatus.Pending)
    //            .ToListAsync();

    //        foreach (var payment in pendingPayments)
    //        {
    //            payment.PaymentStatus = PaymentStatus.Failed;
    //        }

    //        await LogOrderChange(orderId, "ORDER_CANCELLED",
    //            $"Order cancelled. Reason: {reason}", changedByUserId, restaurantId);

    //        await _context.SaveChangesAsync();

    //        if (order.KitchenStatus == KitchenStatus.Preparing || order.KitchenStatus == KitchenStatus.Ready)
    //        {
    //            await NotifyKitchenOrderUpdated(orderId, "ORDER_CANCELLED", restaurantId, order.RestaurantTableID);
    //        }

    //        return Ok(new
    //        {
    //            message = "Order cancelled successfully",
    //            orderID = order.OrderID
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError($"Error cancelling order: {ex.Message}");
    //        return StatusCode(500, "An error occurred while cancelling the order.");
    //    }
    //}
    private async Task LogOrderChange(
    int orderId,
    string changeType,
    string description,
    int? changedByUserId,
    int restaurantId,
    string? oldValues = null,
    string? newValues = null)

    {
        try
        {
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
                ChangedByUserID = userExists ? changedByUserId : null,
                ChangedAt = DateTime.UtcNow,
                OldValues = oldValues,
                NewValues = newValues,
                RestaurantID = restaurantId
            };

            _context.OrderChangeHistory.Add(changeLog);

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to log order change: {ex.Message}");
        }
    }
    private async Task NotifyKitchenOrderUpdated(int orderId, string updateType, int restaurantId, int? tableNo = null)
    {
        try
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return;

            int tableNoForNotification = tableNo.HasValue ? tableNo.Value : 0;

            var notification = new KitchenNotification
            {
                OrderId = orderId,
                TableNo = tableNoForNotification,
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
        }
    }
    private ObjectResult StatusError(int statusCode, string message)
    {
        _logger.LogError(message);
        return StatusCode(statusCode, new { message });
    }

    private string BuildUpiUri(string upiId, string upiName, decimal amount, string transactionId, string note)
    {
        var encodedUpiId = Uri.EscapeDataString(upiId);
        var encodedName = Uri.EscapeDataString(upiName);
        var encodedAmount = amount.ToString("F2");
        var encodedTxnId = Uri.EscapeDataString(transactionId);
        var encodedNote = Uri.EscapeDataString(note);

        return $"upi://pay?pa={encodedUpiId}&pn={encodedName}&am={encodedAmount}&tr={encodedTxnId}&tn={encodedNote}&cu=INR";
    }

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
    [HttpPost("payments/{paymentId}/cash-complete")]
    public async Task<IActionResult> CompleteCashPayment(
     int paymentId,
     [FromQuery] int restaurantId)
    {
        var payment = await _context.Payments
            .Include(p => p.Order)
            .ThenInclude(o => o.Payments)
            .FirstOrDefaultAsync(p =>
                p.PaymentID == paymentId &&
                p.RestaurantID == restaurantId);

        if (payment == null)
            return NotFound("Payment not found");

        if (payment.PaymentStatus == PaymentStatus.Success)
            return BadRequest("Payment already completed");

        payment.PaymentStatus = PaymentStatus.Success;
        payment.PaymentMethod = "Cash";
        payment.CompletedAt = DateTime.UtcNow;

        var order = payment.Order;

        var totalPaid = order.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Success)
            .Sum(p => p.Amount);

        order.PaidAmount = totalPaid;
        order.RemainingAmount = order.TotalAmount - totalPaid;

        if (order.RemainingAmount <= 0)
        {
            order.RemainingAmount = 0;

            if (order.Source == OrderSource.Takeaway)
            {
                // 🔥 Takeaway completes immediately
                order.OrderStatus = OrderStatus.Completed;
                order.ClosedAt = DateTime.UtcNow;
            }
            else
            {
                // 🔥 Dine-in becomes Served but NOT Completed
                if (order.OrderStatus == OrderStatus.Confirmed)
                    order.OrderStatus = OrderStatus.Served;
            }
        }
        else
        {
            // Partial payment for dine-in
            if (order.Source != OrderSource.Takeaway)
                order.OrderStatus = OrderStatus.Served;
        }

        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            paidAmount = order.PaidAmount,
            remainingAmount = order.RemainingAmount,
            orderStatus = order.OrderStatus.ToString()
        });
    }
    [HttpGet("{orderId}/payment-summary")]
    public async Task<IActionResult> GetPaymentSummary(
    int orderId,
    [FromQuery] int restaurantId)
    {
        var order = await _context.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o =>
                o.OrderID == orderId &&
                o.RestaurantID == restaurantId);

        if (order == null)
            return NotFound(new { message = "Order not found." });

        var paidAmount = order.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Success)
            .Sum(p => p.Amount);

        var totalAmount = order.TotalAmount;
        var remainingAmount = totalAmount - paidAmount;

        return Ok(new
        {
            orderId = order.OrderID,
            totalAmount,
            paidAmount,
            remainingAmount
        });
    }

    [HttpPut("payments/{paymentId}/complete")]
    public async Task<IActionResult> CompletePayment(
      int paymentId,
      [FromQuery] int restaurantId)
    {
        var payment = await _context.Payments
            .Include(p => p.Order)
            .ThenInclude(o => o.Payments)
            .FirstOrDefaultAsync(p =>
                p.PaymentID == paymentId &&
                p.RestaurantID == restaurantId);

        if (payment == null)
            return NotFound(new { message = "Payment not found." });

        if (payment.PaymentStatus == PaymentStatus.Success)
            return BadRequest(new { message = "Payment already completed." });

        var order = payment.Order;

        if (order == null)
            return BadRequest(new { message = "Associated order not found." });

        payment.PaymentStatus = PaymentStatus.Success;
        payment.CompletedAt = DateTime.UtcNow;

        var totalPaid = order.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Success)
            .Sum(p => p.Amount);

        order.PaidAmount = totalPaid;
        order.RemainingAmount = order.TotalAmount - totalPaid;

        if (order.RemainingAmount <= 0)
        {
            order.RemainingAmount = 0;

            if (order.Source == OrderSource.Takeaway)
            {
                // 🔥 Takeaway auto-complete
                order.OrderStatus = OrderStatus.Completed;
                order.ClosedAt = DateTime.UtcNow;
            }
            else
            {
                // 🔥 Dine-in stays active
                if (order.OrderStatus == OrderStatus.Confirmed)
                    order.OrderStatus = OrderStatus.Served;
            }
        }

        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Payment marked as success.",
            orderStatus = order.OrderStatus.ToString(),
            paidAmount = order.PaidAmount,
            remainingAmount = order.RemainingAmount
        });
    }





    [HttpPut("{orderId}/change-table")]
    public async Task<IActionResult> ChangeOrderTable(int orderId, [FromQuery] int restaurantId, [FromBody] JsonElement payload)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (!payload.TryGetProperty("newTableNo", out var tableProp) ||
                !payload.TryGetProperty("changedByUserId", out var changedByProp))
            {
                return BadRequest("newTableNo and changedByUserId are required.");
            }

            int newTableNo = tableProp.GetInt32();
            int changedByUserId = changedByProp.GetInt32();

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);

            if (order == null)
            {
                return NotFound(new { message = "Order not found." });
            }

            int oldTableNo = order.RestaurantTableID ?? 0;
            if (oldTableNo == newTableNo)
            {
                return Ok(new { message = "Table number is already set to the requested value." });
            }

            var tableExists = await _context.RestaurantTables
                .AnyAsync(t => t.RestaurantTableID == newTableNo && t.RestaurantID == restaurantId);

            if (!tableExists)
            {
                return BadRequest(new { message = $"Table number {newTableNo} is not valid for this restaurant." });
            }

            order.RestaurantTableID = newTableNo;

            var pendingPayments = await _context.Payments
                .Where(p => p.OrderID == orderId && p.PaymentStatus == PaymentStatus.Pending)
                .ToListAsync();

            foreach (var payment in pendingPayments)
            {
                payment.TableNo = newTableNo;
            }

            string description = $"Table changed from {oldTableNo} to {newTableNo}";
            await LogOrderChange(orderId, "TABLE_CHANGED", description, changedByUserId, restaurantId);

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

    [HttpPut("{orderId}/update")]
    public async Task<IActionResult> UpdateOrder(int orderId, [FromBody] OrderUpdateRequest request, [FromQuery] int restaurantId)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);

            if (order == null)
                return NotFound(new { message = "Order not found." });

            order.CreatedAt = DateTime.Parse(request.CreatedAt).ToUniversalTime();
            order.RestaurantTableID = request.TableNo;
            order.OrderStatus = Enum.Parse<OrderStatus>(request.OrderStatus);
            order.UpdatedAt = DateTime.UtcNow;

            order.OrderItems.Clear();
            foreach (var itemRequest in request.Items)
            {
                var orderItem = new OrderItem
                {
                    ProductID = itemRequest.ProductID,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = itemRequest.UnitPrice,
                    BatchID = 1,
                    RestaurantID = restaurantId,
                    IsPrepared = false,
                    AddedToKitchenAt = DateTime.UtcNow,
                    Customizations = itemRequest.Customizations?.Select(c => new OrderItemCustomization
                    {
                        CustomizationOptionID = c.OptionID,
                        RestaurantID = restaurantId
                    }).ToList() ?? new List<OrderItemCustomization>()
                };
                order.OrderItems.Add(orderItem);
            }

            order.Subtotal = request.Subtotal;
            order.DiscountAmount = request.DiscountAmount;
            order.CGST = request.CGST;
            order.SGST = request.SGST;
            order.ServiceCharge = request.ServiceCharge;
            order.TotalAmount = request.TotalAmount;

            var pendingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderID == orderId && p.PaymentStatus == PaymentStatus.Pending);

            if (pendingPayment != null)
            {
                pendingPayment.Amount = request.TotalAmount;
                pendingPayment.TableNo = request.TableNo;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Order updated successfully", orderId = order.OrderID });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating order {orderId}: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred while updating the order." });
        }
    }
    [HttpGet("takeaway")]
    public async Task<IActionResult> GetTakeawayOrders([FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("restaurantId is required.");

        var orders = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Customizations)
                    .ThenInclude(c => c.CustomizationOption)
            .Include(o => o.Payments)
            .Where(o =>
                o.RestaurantID == restaurantId &&
                o.Source == OrderSource.Takeaway &&
                o.OrderStatus != OrderStatus.Completed &&
                o.OrderStatus != OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        foreach (var order in orders)
        {
            _orderRepository.CalculateOrderAmounts(order);
        }

        return Ok(new
        {
            message = "Takeaway orders fetched successfully",
            orders = orders.Select(order =>
            {
                var summary = GetPaymentSummary(order);

                var latestPayment = order.Payments?
                    .Where(p => p.PaymentStatus == PaymentStatus.Success)
                    .OrderByDescending(p => p.CompletedAt ?? p.CreatedAt)
                    .Select(p => new
                    {
                        method = p.PaymentMethod,
                        status = p.PaymentStatus.ToString(),
                        amount = p.Amount,
                        paidAt = p.CompletedAt
                    })
                    .FirstOrDefault();

                return new
                {
                    orderID = order.OrderID,
                    orderNumber = order.OrderNumber,
                    createdAt = order.CreatedAt,
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
                        orderItemID = item.OrderItemID,
                        productID = item.ProductID,
                        productName = item.Product?.ProductName ?? $"Product {item.ProductID}",
                        quantity = item.Quantity,
                        unitPrice = item.UnitPrice,
                        lineTotal = item.Quantity * item.UnitPrice,

                        // ✅ FULL CUSTOMIZATION SUPPORT
                        customizations = item.Customizations.Select(c => new
                        {
                            customizationOptionID = c.CustomizationOptionID,
                            optionName = c.CustomizationOption?.Name,
                            price = c.CustomizationOption?.FixedPrice ?? 0
                        }).ToList()
                    }).ToList(),

                    latestPayment = latestPayment,

                    paymentType = summary.PaymentType,
                    paymentMethods = summary.PaymentMethods,
                    paidAmount = summary.PaidAmount,
                    remainingAmount = summary.RemainingAmount
                };
            })
        });
    }


    [HttpGet("restaurant/{restaurantId}/details")]
    public async Task<IActionResult> GetRestaurantDetails(int restaurantId)
    {
        try
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.RestaurantID == restaurantId);

            if (restaurant == null)
                return NotFound(new { message = "Restaurant not found" });

            return Ok(new
            {
                name = restaurant.Name,
                address = restaurant.Address,
                description = restaurant.Description,
                upiId = restaurant.UPI_ID,
                upiName = restaurant.UPI_Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching restaurant details: {ex.Message}");
            return StatusCode(500, "An error occurred while fetching restaurant details.");
        }
    }
    [HttpGet("{orderId}/payment-status")]
    public async Task<IActionResult> GetOrderPaymentStatus(
    int orderId,
    [FromQuery] int restaurantId)
    {
        var order = await _context.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o =>
                o.OrderID == orderId &&
                o.RestaurantID == restaurantId);

        if (order == null)
            return NotFound();

        var paid = order.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Success)
            .Sum(p => p.Amount);

        return Ok(new
        {
            total = order.TotalAmount,
            paid,
            remaining = order.TotalAmount - paid,
            isFullyPaid = paid >= order.TotalAmount
        });
    }


    [HttpPost("{orderId}/print-bill")]
    public async Task<IActionResult> PrintBill(int orderId, [FromQuery] int restaurantId)
    {
        // 1️⃣ Fetch data with correct Price/Customization loading
        var order = await _context.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Customizations).ThenInclude(c => c.CustomizationOption)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);

        if (order == null)
            return NotFound(new { message = "Order not found." });

        // 2️⃣ RECALCULATE: Ensure values are fresh and save them
        _orderRepository.CalculateOrderAmounts(order);

        //if (!order.OfferLocked && order.AppliedOfferID == null)
        //{
        //    await _orderRepository.ApplyBestAvailableOfferAsync(order);
        //    _orderRepository.CalculateOrderAmounts(order);
        //}

        // Guard against zero totals before printing
        if (order.TotalAmount <= 0)
        {
            return BadRequest(new { message = "❌ Bill total is zero. Verify item prices." });
        }

        await _context.SaveChangesAsync();

        // 3️⃣ Verify Payment
        var totalPaid = order.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Success)
            .Sum(p => p.Amount);

        // Small tolerance (₹1) for decimal rounding issues
        if (totalPaid < (order.TotalAmount - 1))
        {
            return BadRequest(new
            {
                message = "❌ Payment pending. Complete full payment before printing bill.",
                totalAmount = order.TotalAmount,
                paidAmount = totalPaid
            });
        }

        if (order.ClosedAt != null)
            return BadRequest(new { message = "❌ Bill already printed for this order." });

        // 4️⃣ Get Printer and Build Payload
        var printer = await GetPrinterConfig(restaurantId, "BILL");
        if (printer == null)
            return BadRequest(new { message = "Bill printer not configured." });

        var payload = new
        {
            Type = "BILL",
            PrinterName = printer.PrinterName,
            RestaurantName = printer.HeaderText,
            RestaurantAddress = printer.Address,
            Footer = printer.FooterText ?? "Thank you, visit again",

            Order = new
            {
                OrderNumber = order.OrderNumber.ToString(),
                TableNo = order.RestaurantTableID?.ToString() ?? "0",

                Items = order.OrderItems.Select(i => new
                {
                    Name = i.Product?.ProductName ?? "Item",
                    Qty = i.Quantity,
                    Price = i.UnitPrice,
                    Total = i.Quantity * i.UnitPrice
                }).ToList(),

                Subtotal = order.Subtotal,
                Discount = order.DiscountAmount,
                Tax = order.CGST + order.SGST,

                // ⭐ IMPORTANT FIX
                Total = order.TotalAmount,

                // optional (for debugging / future)
                GrandTotal = order.TotalAmount,

                PaidAmount = totalPaid
            }
        };

        // 5️⃣ Finalize Order
        await SavePrintJob(restaurantId, payload);

        order.OrderStatus = OrderStatus.Completed;
        order.ClosedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "✅ Bill printed successfully",
            total = order.TotalAmount
        });
    }
    [HttpGet("{orderId}/bill")]
    public async Task<IActionResult> DownloadBill(int orderId)
    {
        // 1️⃣ FIX: Use separate paths to load Products and Customizations
        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product) // Path for base item prices
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Customizations)
                    .ThenInclude(c => c.CustomizationOption) // Path for customization prices
            .Include(o => o.RestaurantTable)
            .Include(o => o.AppliedOffer)
            .FirstOrDefaultAsync(o => o.OrderID == orderId);

        if (order == null)
            return NotFound();

        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.RestaurantID == order.RestaurantID);

        // 2️⃣ Recalculate and SAVE immediately to ensure DB isn't out of sync
        _orderRepository.CalculateOrderAmounts(order);
        await _context.SaveChangesAsync();

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(restaurant?.Name ?? "Restaurant Name").Bold().FontSize(22);

                    var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                    col.Item().AlignCenter().Text($"Date: {istTime:dd MMM yyyy | hh:mm tt}")
                        .FontSize(10).FontColor(Colors.Grey.Darken2);

                    col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Column(column =>
                {
                    column.Item().PaddingBottom(10).Text($"Order Number: #{order.OrderNumber}").Bold().FontSize(14);

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

                            // Handle item name with customizations
                            if (item.Customizations.Any())
                            {
                                var customizationNames = string.Join(", ", item.Customizations.Select(c => c.CustomizationOption.Name));
                                table.Cell().Text(text =>
                                {
                                    text.Span(item.Product?.ProductName ?? "Unknown Item");
                                    text.EmptyLine();
                                    text.Span($"Custom: {customizationNames}").FontColor(Colors.Grey.Medium).FontSize(8);
                                });
                            }
                            else
                            {
                                table.Cell().Text(item.Product?.ProductName ?? "Unknown Item");
                            }

                            table.Cell().AlignRight().Text(item.Quantity.ToString());
                            table.Cell().AlignRight().Text($"₹{item.UnitPrice:N2}");
                            table.Cell().AlignRight().Text($"₹{(item.UnitPrice * item.Quantity):N2}");
                        }
                    });

                    // Totals Section
                    column.Item().PaddingTop(15).AlignRight().Text(text =>
                    {
                        text.Span("Subtotal: ").Bold();
                        text.Span($"₹{order.Subtotal:N2}");
                        text.EmptyLine();

                        if (order.DiscountAmount > 0)
                        {
                            text.Span($"Discount ({order.AppliedOffer?.Description ?? "Offer"}): ").Bold();
                            text.Span($"- ₹{order.DiscountAmount:N2}");
                            text.EmptyLine();
                        }

                        text.Span("Taxes (GST): ").Bold();
                        text.Span($"₹{(order.CGST + order.SGST):N2}");
                        text.EmptyLine();

                        text.Span("Total: ").Bold().FontSize(14);
                        text.Span($"₹{order.TotalAmount:N2}").FontSize(14);
                    });
                });

                page.Footer().AlignCenter().Text("Thank you! Visit us again.").FontSize(10).Italic();
            });
        }).GeneratePdf();

        return File(pdfBytes, "application/pdf", $"Bill_Order_{order.OrderNumber}.pdf");
    }
    //[HttpPost("{orderId}/print-bill")]
    //public async Task<IActionResult> PrintBill(int orderId, [FromQuery] int restaurantId)
    //{
    //    // 1️⃣ Fetch order with related data and payments
    //    var order = await _context.Orders
    //        .Include(o => o.OrderItems)
    //            .ThenInclude(oi => oi.Product)
    //        .Include(o => o.Payments)
    //        .Include(o => o.RestaurantTable)
    //        .FirstOrDefaultAsync(o =>
    //            o.OrderID == orderId &&
    //            o.RestaurantID == restaurantId);

    //    if (order == null)
    //    {
    //        return NotFound(new { message = "Order not found." });
    //    }

    //    // 2️⃣ Calculate total successful payments
    //    var totalPaid = order.Payments
    //        .Where(p => p.PaymentStatus == PaymentStatus.Success)
    //        .Sum(p => p.Amount);

    //    // 3️⃣ Calculate remaining amount
    //    var remainingAmount = order.TotalAmount - totalPaid;

    //    // 🔥 FIX: Use a ₹1.00 tolerance grace. 
    //    // This prevents orders from getting stuck due to ₹0.60 or similar rounding issues.
    //    if (remainingAmount > 1.0m)
    //    {
    //        return BadRequest(new
    //        {
    //            message = "❌ Payment pending. Complete full payment before printing bill.",
    //            totalAmount = order.TotalAmount,
    //            paidAmount = totalPaid,
    //            remainingAmount = remainingAmount
    //        });
    //    }

    //    // 4️⃣ Get BILL printer configuration
    //    var printer = await GetPrinterConfig(restaurantId, "BILL");

    //    if (printer == null)
    //    {
    //        return BadRequest(new
    //        {
    //            message = "Bill printer not configured."
    //        });
    //    }

    //    // 5️⃣ Build Print Payload (Labels for takeaway vs Dine-in)
    //    string tableLabel = order.Source == OrderSource.Takeaway
    //        ? "TAKEAWAY"
    //        : order.RestaurantTableID?.ToString() ?? "0";

    //    string orderTypeLabel = order.Source == OrderSource.Takeaway
    //        ? "Takeaway"
    //        : "Dine-In";

    //    var payload = new
    //    {
    //        Type = "BILL",
    //        PrinterName = printer.PrinterName,
    //        RestaurantName = printer.HeaderText,
    //        RestaurantAddress = printer.Address,
    //        Footer = string.IsNullOrWhiteSpace(printer.FooterText)
    //            ? "Thank you, visit again"
    //            : printer.FooterText,

    //        Order = new
    //        {
    //            OrderNumber = order.OrderNumber.ToString(),
    //            OrderType = orderTypeLabel,
    //            TableNo = tableLabel,

    //            Items = order.OrderItems.Select(i => new
    //            {
    //                Name = i.Product?.ProductName ?? "Item",
    //                Qty = i.Quantity,
    //                Price = i.UnitPrice,
    //                Total = i.Quantity * i.UnitPrice
    //            }).ToList(),

    //            Subtotal = order.Subtotal,
    //            Discount = order.DiscountAmount,
    //            CGST = order.CGST,
    //            SGST = order.SGST,
    //            ServiceCharge = order.ServiceCharge,
    //            GrandTotal = order.TotalAmount,
    //            PaidAmount = totalPaid
    //        }
    //    };

    //    // 6️⃣ Save print job to queue
    //    await SavePrintJob(restaurantId, payload);

    //    // 7️⃣ CLOSE ORDER: Change status to Completed to move it to History
    //    order.OrderStatus = OrderStatus.Completed;
    //    order.ClosedAt = DateTime.UtcNow;
    //    order.UpdatedAt = DateTime.UtcNow;

    //    await _context.SaveChangesAsync();

    //    return Ok(new
    //    {
    //        success = true,
    //        message = "✅ Bill printed and order moved to history.",
    //        orderNumber = order.OrderNumber,
    //        orderType = orderTypeLabel,
    //        totalAmount = order.TotalAmount,
    //        paidAmount = totalPaid
    //    });
    //}







    [HttpPost("{orderId}/print-preview")]
    public async Task<IActionResult> PrintPreviewBill(
    int orderId,
    [FromQuery] int restaurantId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Customizations)
            .ThenInclude(c => c.CustomizationOption)
            .FirstOrDefaultAsync(o => o.OrderID == orderId && o.RestaurantID == restaurantId);

        if (order == null)
            return NotFound("Order not found");

        _orderRepository.CalculateOrderAmounts(order);

        var printer = await GetPrinterConfig(restaurantId, "BILL");

        if (printer == null)
            return BadRequest("Bill printer not configured");

        var payload = new
        {
            Type = "PREVIEW_BILL",
            PrinterName = printer.PrinterName,
            RestaurantName = printer.HeaderText,
            RestaurantAddress = printer.Address,
            Footer = printer.FooterText ?? "",

            Order = new
            {
                OrderNumber = order.OrderNumber.ToString(),
                TableNo = order.RestaurantTableID?.ToString() ?? "0",

                Items = order.OrderItems.Select(i => new
                {
                    Name = i.Product?.ProductName ?? "Item",
                    Qty = i.Quantity,
                    Price = i.UnitPrice,
                    Total = i.Quantity * i.UnitPrice
                }).ToList(),

                Subtotal = order.Subtotal,
                Discount = order.DiscountAmount,
                Tax = order.CGST + order.SGST,
                Total = order.TotalAmount
            }
        };

        await SavePrintJob(restaurantId, payload);

        return Ok(new
        {
            message = "Preview bill printed"
        });
    }



    private (DateTime startUtc, DateTime endUtc) NormalizeDateRange(DateTime start, DateTime end)
    {
        var startUtc = DateTime.SpecifyKind(start.Date, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(end.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        return (startUtc, endUtc);
    }


    [HttpGet("manager/reports/overview")]
    public async Task<IActionResult> GetManagerOverview(
     [FromQuery] int restaurantId,
     [FromQuery] string? orderType = null)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        var query = _context.Orders
            .Include(o => o.Payments)
            .Where(o => o.RestaurantID == restaurantId);

        if (!string.IsNullOrEmpty(orderType) &&
            Enum.TryParse<OrderSource>(orderType, true, out var source))
        {
            query = query.Where(o => o.Source == source);
        }

        var orders = await query.ToListAsync();

        var todayOrders = orders
            .Where(o => o.CreatedAt >= todayUtc && o.CreatedAt < tomorrowUtc)
            .ToList();

        var liveOrders = orders.Count(o =>
            o.OrderStatus != OrderStatus.Completed &&
            o.OrderStatus != OrderStatus.Cancelled);

        var todayRevenue = todayOrders.Sum(o =>
            o.Payments.Where(p => p.PaymentStatus == PaymentStatus.Success)
                      .Sum(p => p.Amount));

        var paidTodayOrders = todayOrders
            .Where(o => o.Payments.Any(p => p.PaymentStatus == PaymentStatus.Success))
            .Count();

        return Ok(new
        {
            liveOrders,
            todayRevenue,
            todayOrders = paidTodayOrders,
            avgOrderValue = paidTodayOrders > 0
                ? todayRevenue / paidTodayOrders
                : 0
        });
    }


    [HttpGet("manager/reports/sales")]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] int restaurantId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? orderType = null)
    {
        var startUtc = startDate.Date;
        var endUtc = endDate.Date.AddDays(1).AddTicks(-1);

        var query = _context.Orders
            .Include(o => o.Payments)
            .Where(o =>
                o.RestaurantID == restaurantId &&
                o.CreatedAt >= startUtc &&
                o.CreatedAt <= endUtc);

        if (!string.IsNullOrEmpty(orderType) &&
            Enum.TryParse<OrderSource>(orderType, true, out var source))
        {
            query = query.Where(o => o.Source == source);
        }

        var orders = await query.ToListAsync();

        var paidOrders = orders
            .Where(o => o.Payments.Any(p => p.PaymentStatus == PaymentStatus.Success))
            .ToList();

        var grossRevenue = paidOrders.Sum(o =>
            o.Payments.Where(p => p.PaymentStatus == PaymentStatus.Success)
                      .Sum(p => p.Amount));

        return Ok(new
        {
            totalOrders = paidOrders.Count,
            grossRevenue,
            discount = paidOrders.Sum(o => o.DiscountAmount),
            tax = paidOrders.Sum(o => o.CGST + o.SGST),
            netRevenue = grossRevenue
        });
    }


    [HttpGet("manager/reports/orders")]
    public async Task<IActionResult> GetOrderReport(
        [FromQuery] int restaurantId,
        [FromQuery] string? orderType = null)
    {
        var query = _context.Orders
            .Where(o => o.RestaurantID == restaurantId);

        if (!string.IsNullOrEmpty(orderType) &&
            Enum.TryParse<OrderSource>(orderType, true, out var source))
        {
            query = query.Where(o => o.Source == source);
        }

        var orders = await query.ToListAsync();

        var completedOrders = orders
            .Where(o => o.OrderStatus == OrderStatus.Completed && o.ClosedAt != null)
            .ToList();

        double avgMinutes = completedOrders.Any()
            ? completedOrders.Average(o => (o.ClosedAt.Value - o.CreatedAt).TotalMinutes)
            : 0;

        return Ok(new
        {
            totalOrders = orders.Count,
            completedOrders = completedOrders.Count,
            cancelledOrders = orders.Count(o => o.OrderStatus == OrderStatus.Cancelled),
            liveOrders = orders.Count(o =>
                o.OrderStatus != OrderStatus.Completed &&
                o.OrderStatus != OrderStatus.Cancelled),
            avgOrderMinutes = Math.Round(avgMinutes, 1)
        });
    }

    [HttpGet("manager/reports/items")]
    public async Task<IActionResult> GetItemReport(
     int restaurantId,
     DateTime startDate,
     DateTime endDate)
    {
        var (startUtc, endUtc) = NormalizeDateRange(startDate, endDate);

        var items = await _context.OrderItems
            .Include(i => i.Product)
            .Include(i => i.Order)
                .ThenInclude(o => o.Payments)
            .Include(i => i.Order)
                .ThenInclude(o => o.AppliedOffer)
            .Where(i =>
                i.Order.RestaurantID == restaurantId &&
                i.Order.CreatedAt >= startUtc &&
                i.Order.CreatedAt <= endUtc &&
                i.Order.Payments.Any(p => p.PaymentStatus == PaymentStatus.Success))
            .ToListAsync();

        var reportData = items
            .Where(i => i.Product != null)
            .GroupBy(i => i.Product.ProductName)
            .Select(g =>
            {
                var gross = g.Sum(x => x.Quantity * x.UnitPrice);

                var discount = g.Sum(x =>
                    x.Order.Subtotal > 0
                        ? ((x.Quantity * x.UnitPrice) / x.Order.Subtotal) * x.Order.DiscountAmount
                        : 0
                );

                return new
                {
                    itemName = g.Key,
                    quantitySold = g.Sum(x => x.Quantity),
                    grossRevenue = gross,
                    totalDiscount = discount,
                    netRevenue = gross - discount
                };
            })
            .OrderByDescending(x => x.quantitySold)
            .ToList();

        return Ok(reportData);
    }




    [HttpGet("manager/reports/categories")]
    public async Task<IActionResult> GetCategoryReport(int restaurantId)
    {
        var data = await _context.OrderItems
            .Include(i => i.Product)
                .ThenInclude(p => p.Category)
            .Include(i => i.Order)
                .ThenInclude(o => o.Payments)
            .Where(i => i.Order.RestaurantID == restaurantId &&
                        i.Order.Payments.Any(p => p.PaymentStatus == PaymentStatus.Success))
            .ToListAsync();

        var groupedData = data
            .Where(i => i.Product?.Category != null)
            .GroupBy(i => i.Product.Category.CategoryName)
            .Select(g => new
            {
                category = g.Key,
                totalQuantity = g.Sum(x => x.Quantity),
                revenue = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.revenue)
            .ToList();

        return Ok(groupedData);
    }


    [HttpGet("manager/reports/live-orders")]
    public async Task<IActionResult> GetLiveOrders(
        [FromQuery] int restaurantId,
        [FromQuery] string? orderType = null)
    {
        var query = _context.Orders
            .Include(o => o.RestaurantTable)
            .Where(o =>
                o.RestaurantID == restaurantId &&
                o.OrderStatus != OrderStatus.Completed &&
                o.OrderStatus != OrderStatus.Cancelled);

        if (!string.IsNullOrEmpty(orderType) &&
            Enum.TryParse<OrderSource>(orderType, true, out var source))
        {
            query = query.Where(o => o.Source == source);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(orders.Select(o => new
        {
            orderID = o.OrderID,
            orderNumber = o.OrderNumber,
            orderType = o.Source.ToString(),
            table = o.Source == OrderSource.Takeaway
                ? "Takeaway"
                : o.RestaurantTable?.TableName,
            status = o.OrderStatus.ToString(),
            total = o.TotalAmount,
            minutesAgo = (int)(DateTime.UtcNow - o.CreatedAt).TotalMinutes
        }));
    }
    [HttpGet("manager/reports/past-orders")]
    public async Task<IActionResult> GetPastOrders(
     [FromQuery] int restaurantId,
     [FromQuery] DateTime? startDate,
     [FromQuery] DateTime? endDate,
     [FromQuery] string? orderType = null)
    {
        IQueryable<Order> query = _context.Orders
            .Include(o => o.Payments)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.RestaurantTable)
            .Where(o => o.RestaurantID == restaurantId);

        // 🔹 Order Type Filter
        if (!string.IsNullOrEmpty(orderType) &&
            Enum.TryParse<OrderSource>(orderType, true, out var source))
        {
            query = query.Where(o => o.Source == source);
        }

        // 🔹 Date Filter
        if (startDate.HasValue && endDate.HasValue)
        {
            var startUtc = startDate.Value.Date;
            var endUtc = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.CreatedAt >= startUtc && o.CreatedAt <= endUtc);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var result = orders.Select(o =>
        {
            var successfulPayments = o.Payments
                .Where(p => p.PaymentStatus == PaymentStatus.Success)
                .ToList();

            var paidAmount = successfulPayments.Sum(p => p.Amount);

            var cashPaid = successfulPayments
                .Where(p => p.PaymentMethod == "Cash")
                .Sum(p => p.Amount);

            var upiPaid = successfulPayments
                .Where(p => p.PaymentMethod == "UPI")
                .Sum(p => p.Amount);

            var remaining = Math.Max(o.TotalAmount - paidAmount, 0);

            // 🔥 Payment Method Display (Cash / UPI / Cash + UPI / Pending)
            var paymentMethods = successfulPayments
                .Select(p => p.PaymentMethod ?? "Unknown")
                .Distinct()
                .ToList();

            string paymentMethodDisplay =
                !paymentMethods.Any() ? "Pending" :
                paymentMethods.Count == 1 ? paymentMethods.First() :
                string.Join(" + ", paymentMethods);

            return new
            {
                orderID = o.OrderID,
                orderNumber = o.OrderNumber,
                orderType = o.Source.ToString(),
                date = o.CreatedAt,

                table = o.Source == OrderSource.Takeaway
                    ? "Takeaway"
                    : o.RestaurantTable?.TableName ?? "-",

                status = o.OrderStatus.ToString(),
                total = o.TotalAmount,

                cashPaid,
                upiPaid,
                paid = paidAmount,
                remaining,
                paymentMethod = paymentMethodDisplay,

                payments = successfulPayments.Select(p => new
                {
                    paymentMethod = p.PaymentMethod,
                    amount = p.Amount,
                    channel = p.PaymentChannel.ToString(),
                    createdAt = p.CreatedAt,
                    completedAt = p.CompletedAt
                }),

                items = o.OrderItems.Select(i =>
                {
                    var gross = i.Quantity * i.UnitPrice;

                    var proportionalDiscount =
                        o.Subtotal > 0
                            ? Math.Round((gross / o.Subtotal) * o.DiscountAmount, 2)
                            : 0;

                    return new
                    {
                        itemName = i.Product?.ProductName ?? "Item",
                        quantity = i.Quantity,
                        originalPrice = gross,
                        discountAmount = proportionalDiscount,
                        finalPrice = gross - proportionalDiscount
                    };
                })
            };
        });

        return Ok(result);
    }
    [HttpPut("{orderId}/takeaway/pay-and-serve")]
    public async Task<IActionResult> PayAndServeTakeaway(
    int orderId,
    [FromQuery] int restaurantId,
    [FromBody] JsonElement payload)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = await _context.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o =>
                    o.OrderID == orderId &&
                    o.RestaurantID == restaurantId &&
                    o.Source == OrderSource.Takeaway);

            if (order == null)
                return NotFound("Takeaway order not found.");

            if (order.OrderStatus == OrderStatus.Completed)
                return BadRequest("Order already completed.");

            // 🔹 Extract method + amount
            string method = payload.GetProperty("method").GetString() ?? "Cash";
            decimal amount = payload.GetProperty("amount").GetDecimal();

            if (amount <= 0)
                return BadRequest("Invalid payment amount.");

            // 🔹 Calculate already paid
            var alreadyPaid = order.Payments
                .Where(p => p.PaymentStatus == PaymentStatus.Success)
                .Sum(p => p.Amount);

            var remaining = order.TotalAmount - alreadyPaid;

            if (amount > remaining)
                amount = remaining;

            // 🔹 Create Payment record
            var payment = new Payment
            {
                OrderID = orderId,
                TableNo = 0,
                Amount = amount,
                PaymentMethod = method,
                PaymentStatus = PaymentStatus.Success,
                RestaurantID = restaurantId,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);

            alreadyPaid += amount;
            remaining = order.TotalAmount - alreadyPaid;

            // 🔥 STRICT RULE: Full payment required for takeaway
            if (remaining > 0)
            {
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Partial payment received.",
                    paidAmount = alreadyPaid,
                    remainingAmount = remaining,
                    orderStatus = order.OrderStatus.ToString()
                });
            }

            // 🔥 FULL PAYMENT → Auto Serve + Complete
            order.OrderStatus = OrderStatus.Completed;
            order.ClosedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Takeaway payment completed and order served.",
                orderStatus = order.OrderStatus.ToString(),
                paidAmount = alreadyPaid
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "PayAndServeTakeaway failed");
            return StatusCode(500, "Failed to process takeaway payment.");
        }
    }


    [HttpGet("manager/reports/dashboard-stats")]
    public async Task<IActionResult> GetManagerDashboardStats(
     [FromQuery] int restaurantId,
     [FromQuery] string? orderType = null)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        var query = _context.Orders
            .Include(o => o.Payments)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o =>
                o.RestaurantID == restaurantId &&
                o.CreatedAt >= todayUtc &&
                o.CreatedAt < tomorrowUtc);

        if (!string.IsNullOrEmpty(orderType) &&
            Enum.TryParse<OrderSource>(orderType, true, out var source))
        {
            query = query.Where(o => o.Source == source);
        }

        var orders = await query.ToListAsync();

        var paidOrders = orders
            .Where(o => o.Payments.Any(p => p.PaymentStatus == PaymentStatus.Success))
            .ToList();

        var totalRevenue = paidOrders.Sum(o =>
            o.Payments.Where(p => p.PaymentStatus == PaymentStatus.Success)
                      .Sum(p => p.Amount));

        var totalDiscount = paidOrders.Sum(o => o.DiscountAmount);

        var topItems = paidOrders
            .SelectMany(o => o.OrderItems)
            .GroupBy(i => i.Product.ProductName)
            .Select(g => new
            {
                name = g.Key,
                qty = g.Sum(x => x.Quantity),
                grossRevenue = g.Sum(x => x.Quantity * x.UnitPrice),
                totalDiscount = 0.0,
                netRevenue = g.Sum(x => x.Quantity * x.UnitPrice),
            })
            .OrderByDescending(x => x.qty)
            .Take(5)
            .ToList();

        var totalNet = totalRevenue;

        return Ok(new
        {
            totalRevenue = totalNet,
            totalDiscount,
            totalOrders = paidOrders.Count,
            avgOrderValue = paidOrders.Count > 0
                ? totalNet / paidOrders.Count
                : 0,
            topItems
        });
    }
    [HttpPut("{orderId}/apply-offer")]
    public async Task<IActionResult> ApplyOfferManually(
    int orderId,
    [FromQuery] int restaurantId,
    [FromBody] JsonElement payload)
    {
        if (!payload.TryGetProperty("offerId", out var offerProp))
            return BadRequest("offerId required");

        int offerId = offerProp.GetInt32();

        await using var tx = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = await _orderRepository
                .GetOrderByIdWithItemsAsync(orderId, restaurantId);

            if (order == null)
                return NotFound("Order not found");

            if (order.OrderStatus == OrderStatus.Completed ||
                order.OrderStatus == OrderStatus.Cancelled)
                return BadRequest("Cannot apply offer to closed order");

            // 🔹 Force clear existing offer
            order.AppliedOfferID = null;
            order.DiscountAmount = 0;

            _orderRepository.CalculateOrderAmounts(order);

            // 🔹 Apply specific offer (not best offer)
            var success = await _orderRepository
                .ApplySpecificOfferAsync(order, offerId);

            if (!success)
                return BadRequest("Offer not valid for this order");

            _orderRepository.CalculateOrderAmounts(order);

            order.OfferLocked = false; // allow recalculation until confirm

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new
            {
                message = "Offer applied successfully",
                discount = order.DiscountAmount,
                total = order.TotalAmount
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Manual offer apply failed");
            return StatusCode(500, "Failed to apply offer");
        }
    }
    [HttpPut("{orderId}/remove-offer")]
    public async Task<IActionResult> RemoveOffer(
    int orderId,
    [FromQuery] int restaurantId)
    {
        var order = await _orderRepository
            .GetOrderByIdWithItemsAsync(orderId, restaurantId);

        if (order == null)
            return NotFound();

        order.AppliedOfferID = null;
        order.DiscountAmount = 0;
        order.OfferLocked = false;

        _orderRepository.CalculateOrderAmounts(order);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Offer removed",
            total = order.TotalAmount
        });
    }



    [HttpGet("with-waiter")]
    public async Task<IActionResult> GetOrdersWithWaiters([FromQuery] int restaurantId)
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Customizations)
                .ThenInclude(c => c.CustomizationOption)
            .Include(o => o.Payments)
            .Where(o => o.RestaurantID == restaurantId)
            .ToListAsync();

        foreach (var order in orders)
        {
            _orderRepository.CalculateOrderAmounts(order);
        }

        return Ok(new
        {
            message = "Orders fetched successfully!",
            orders = orders.Select(order =>
            {
                var summary = GetPaymentSummary(order);

                var latestPayment = order.Payments?
                    .Where(p => p.PaymentStatus == PaymentStatus.Success)
                    .OrderByDescending(p => p.CompletedAt ?? p.CreatedAt)
                    .Select(p => new
                    {
                        method = p.PaymentMethod,
                        status = p.PaymentStatus.ToString(),
                        amount = p.Amount,
                        paidAt = p.CompletedAt
                    })
                    .FirstOrDefault();

                return new
                {
                    orderID = order.OrderID,
                    orderNumber = order.OrderNumber,
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
                        orderItemID = item.OrderItemID,
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
                    latestPayment = latestPayment,
                    paymentType = summary.PaymentType,
                    paymentMethods = summary.PaymentMethods,
                    paidAmount = summary.PaidAmount,
                    remainingAmount = summary.RemainingAmount
                };
            })
        });
    }


    private async Task SavePrintJob(int restaurantId, object payload)
    {
        _logger.LogInformation($"📥 SavePrintJob START | RestaurantID={restaurantId}");

        try
        {
            // 🔹 Serialize payload
            string jsonPayload = JsonConvert.SerializeObject(payload);

            _logger.LogInformation($"📄 Payload JSON: {jsonPayload}");

            // 🔹 Create print job
            var printJob = new PrintJob
            {
                RestaurantID = restaurantId,
                PayloadJson = jsonPayload,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("🧾 PrintJob object created");

            // 🔹 Add to DB context
            _context.PrintJobs.Add(printJob);

            _logger.LogInformation("📌 PrintJob added to DbContext");

            // 🔹 Save to DB
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ PrintJob SAVED | ID={printJob.PrintJobID}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ SavePrintJob FAILED");

            // Optional: rethrow so API fails visibly
            throw;
        }

        _logger.LogInformation("🏁 SavePrintJob END");
    }

    // Payment summary helper - copy into your controller (private)
    private (string PaymentType, string PaymentMethods, decimal PaidAmount, decimal RemainingAmount) GetPaymentSummary(Order order)
    {
        if (order == null) return ("Pending", "-", 0m, 0m);

        var payments = order.Payments ?? new List<Payment>();
        var successfulPayments = payments
            .Where(p => p.PaymentStatus == PaymentStatus.Success)
            .OrderBy(p => p.CompletedAt ?? p.CreatedAt) // chronological
            .ToList();

        decimal paidAmount = successfulPayments.Sum(p => p.Amount);
        decimal remaining = (order.TotalAmount <= 0) ? 0m : Math.Max(order.TotalAmount - paidAmount, 0m);

        string paymentMethods;
        if (!successfulPayments.Any())
        {
            paymentMethods = "-";
        }
        else
        {
            paymentMethods = string.Join(" + ",
                successfulPayments
                    .Select(p => string.IsNullOrWhiteSpace(p.PaymentMethod) ? "Unknown" : p.PaymentMethod)
                    .Distinct());
        }

        // Business rule: treat any multi-method payment as PARTIAL for manager visibility,
        // even if paidAmount == TotalAmount. Single successful payment exactly equal to total => FULL.
        string paymentType;
        if (!successfulPayments.Any())
        {
            paymentType = "Pending";
        }
        else if (successfulPayments.Count == 1 && paidAmount >= order.TotalAmount && order.TotalAmount > 0)
        {
            paymentType = "Full";
        }
        else
        {
            paymentType = "Partial";
        }

        return (paymentType, paymentMethods, paidAmount, remaining);
    }

    private async Task<RestaurantPrinter?> GetPrinterConfig(
    int restaurantId,
    string type)
    {
        return await _context.RestaurantPrinters
            .FirstOrDefaultAsync(p =>
                p.RestaurantID == restaurantId &&
                p.PrinterType == type &&
                p.IsActive);
    }
}
