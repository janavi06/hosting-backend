using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Models;
using Restaurant_System.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class TableManagementController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TableManagementController> _logger;

    public TableManagementController(ApplicationDbContext context, ILogger<TableManagementController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetTableStatus([FromQuery] int restaurantId)
    {
        try
        {
            var tables = await _context.TableManagement
                .Include(t => t.RestaurantTable)
                .Include(t => t.CurrentOrder)
                .Where(t => t.RestaurantID == restaurantId)
                .OrderBy(t => t.RestaurantTableID)
                .ToListAsync();

            var floorPlan = tables.Select(t => new
            {
                t.TableManagementID,
                t.RestaurantTableID,
                TableName = t.RestaurantTable.TableName,
                t.Status,
                t.Section,
                t.SeatingCapacity,
                t.SpecialFeatures,
                t.XPosition,
                t.YPosition,
                CurrentOrder = t.CurrentOrderID.HasValue ? new
                {
                    t.CurrentOrder.OrderID,
                    t.CurrentOrder.OrderStatus
                } : null,
                OccupiedDuration = t.OccupiedSince.HasValue ?
                    (DateTime.UtcNow - t.OccupiedSince.Value).TotalMinutes : 0
            }).ToList();

            return Ok(new { message = "Table status fetched successfully", data = floorPlan });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching table status: {ex.Message}");
            return StatusCode(500, "An error occurred while fetching table status.");
        }
    }

    [HttpPut("status/{tableId}")]
    public async Task<IActionResult> UpdateTableStatus(int tableId, [FromBody] TableStatus status, [FromQuery] int restaurantId)
    {
        try
        {
            var table = await _context.TableManagement
                .FirstOrDefaultAsync(t => t.RestaurantTableID == tableId && t.RestaurantID == restaurantId);

            if (table == null) return NotFound();

            table.Status = status;
            table.LastUpdated = DateTime.UtcNow;

            if (status == TableStatus.Occupied && !table.OccupiedSince.HasValue)
            {
                table.OccupiedSince = DateTime.UtcNow;
            }
            else if (status != TableStatus.Occupied)
            {
                table.OccupiedSince = null;
                table.CurrentOrderID = null;
                table.ReservedByCustomerID = null;
                table.ReservationTime = null;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Table {tableId} status updated to {status}", data = table });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating table status: {ex.Message}");
            return StatusCode(500, "An error occurred while updating table status.");
        }
    }

    [HttpGet("reservations")]
    public async Task<IActionResult> GetReservations([FromQuery] int restaurantId, [FromQuery] DateTime? date = null)
    {
        try
        {
            var query = _context.Reservations
                .Include(r => r.RestaurantTable)
                .Where(r => r.RestaurantID == restaurantId);

            if (date.HasValue)
            {
                query = query.Where(r => r.ReservationTime.Date == date.Value.Date);
            }

            var reservations = await query
                .OrderBy(r => r.ReservationTime)
                .ToListAsync();

            return Ok(new { message = "Reservations fetched successfully", data = reservations });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching reservations: {ex.Message}");
            return StatusCode(500, "An error occurred while fetching reservations.");
        }
    }

    [HttpPost("reservations")]
    public async Task<IActionResult> CreateReservation([FromBody] Reservation reservation)
    {
        try
        {
            // Check if table is available
            var existingReservation = await _context.Reservations
                .AnyAsync(r => r.RestaurantTableID == reservation.RestaurantTableID &&
                              r.ReservationTime.Date == reservation.ReservationTime.Date &&
                              r.Status == "Confirmed");

            if (existingReservation)
            {
                return BadRequest(new { message = "Table is already reserved for this time." });
            }

            reservation.CreatedAt = DateTime.UtcNow;
            reservation.UpdatedAt = DateTime.UtcNow;

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Update table status
            var table = await _context.TableManagement
                .FirstOrDefaultAsync(t => t.RestaurantTableID == reservation.RestaurantTableID);

            if (table != null)
            {
                table.Status = TableStatus.Reserved;
                table.ReservationTime = reservation.ReservationTime;
                table.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Reservation created successfully", data = reservation });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating reservation: {ex.Message}");
            return StatusCode(500, "An error occurred while creating reservation.");
        }
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetTableAnalytics([FromQuery] int restaurantId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.RestaurantID == restaurantId &&
                           o.CreatedAt >= startDate &&
                           o.CreatedAt <= endDate &&
                           o.OrderStatus != OrderStatus.Cancelled)
                .ToListAsync();

            // ✅ ADD NULL CHECK
            if (orders == null || !orders.Any())
            {
                return Ok(new
                {
                    message = "No order data found for analytics",
                    tablePerformance = new List<object>(),
                    occupancyRate = 0,
                    summary = new
                    {
                        TotalTables = 0,
                        MostProfitableTable = (object)null,
                        LeastProfitableTable = (object)null
                    }
                });
            }

            var tablePerformance = orders
                .GroupBy(o => o.RestaurantTableID)
                .Select(g => new
                {
                    TableID = g.Key,
                    TotalOrders = g.Count(),
                    TotalRevenue = g.Sum(o => o.TotalAmount),
                    AverageOrderValue = g.Average(o => o.TotalAmount),
                    AverageOccupancyTime = g.Average(o => o.ClosedAt.HasValue ?
                        (o.ClosedAt.Value - o.CreatedAt).TotalMinutes : 0)
                })
                .OrderByDescending(t => t.TotalRevenue)
                .ToList();

            var occupancyRate = await CalculateOccupancyRate(restaurantId, startDate.Value, endDate.Value);

            return Ok(new
            {
                message = "Table analytics fetched successfully",
                tablePerformance,
                occupancyRate,
                summary = new
                {
                    TotalTables = tablePerformance.Count,
                    MostProfitableTable = tablePerformance.FirstOrDefault(),
                    LeastProfitableTable = tablePerformance.LastOrDefault()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching table analytics: {ex.Message}");
            return StatusCode(500, "An error occurred while fetching table analytics.");
        }
    }

    private async Task<decimal> CalculateOccupancyRate(int restaurantId, DateTime startDate, DateTime endDate)
    {
        var totalHours = (endDate - startDate).TotalHours * 10; // Assuming 10 tables
        var occupiedHours = await _context.Orders
            .Where(o => o.RestaurantID == restaurantId &&
                       o.CreatedAt >= startDate &&
                       o.CreatedAt <= endDate &&
                       o.ClosedAt.HasValue)
            .SumAsync(o => (o.ClosedAt.Value - o.CreatedAt).TotalHours);

        return totalHours > 0 ? (decimal)(occupiedHours / totalHours * 100) : 0;
    }
}