using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Models;
using Restaurant_System.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(ApplicationDbContext context, ILogger<CustomerController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers([FromQuery] int restaurantId, [FromQuery] string? search = null)
    {
        try
        {
            var query = _context.Customers
                .Where(c => c.RestaurantID == restaurantId);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search) || c.Phone.Contains(search) || c.Email.Contains(search));
            }

            var customers = await query
                .OrderByDescending(c => c.TotalSpent)
                .ToListAsync();

            return Ok(new { message = "Customers fetched successfully", data = customers });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching customers: {ex.Message}");
            return StatusCode(500, "An error occurred while fetching customers.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddCustomer([FromBody] Customer customer)
    {
        try
        {
            customer.FirstVisit = DateTime.UtcNow;
            customer.LastVisit = DateTime.UtcNow;
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Customer added successfully", data = customer });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding customer: {ex.Message}");
            return StatusCode(500, "An error occurred while adding customer.");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomerDetail(int id, [FromQuery] int restaurantId)
    {
        try
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerID == id && c.RestaurantID == restaurantId);

            // ✅ ADD NULL CHECK
            if (customer == null)
            {
                return NotFound(new { message = "Customer not found" });
            }

            // Get customer order history
            var orders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Where(o => o.UserID == id && o.RestaurantID == restaurantId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(20)
                .ToListAsync();

            // ✅ ADD NULL CHECK FOR ORDERS IN CALCULATIONS
            var customerLifetimeValue = CalculateCustomerLTV(customer);
            var visitFrequency = CalculateVisitFrequency(customer);
            var preferredItems = GetPreferredItems(orders ?? new List<Order>()); // ✅ PASS EMPTY LIST IF NULL

            return Ok(new
            {
                message = "Customer details fetched successfully",
                customer = customer,
                orders = orders,
                analytics = new
                {
                    LifetimeValue = customerLifetimeValue,
                    VisitFrequency = visitFrequency,
                    AverageOrderValue = customer.AverageOrderValue,
                    PreferredItems = preferredItems
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching customer details: {ex.Message}");
            return StatusCode(500, "An error occurred while fetching customer details.");
        }
    }

    [HttpPost("feedback")]
    public async Task<IActionResult> SubmitFeedback([FromBody] CustomerFeedback feedback)
    {
        try
        {
            feedback.CreatedAt = DateTime.UtcNow;
            _context.CustomerFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            // Update customer stats if associated with order
            if (feedback.OrderID.HasValue)
            {
                await UpdateCustomerStats(feedback.CustomerID, feedback.Rating);
            }

            return Ok(new { message = "Feedback submitted successfully", data = feedback });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error submitting feedback: {ex.Message}");
            return StatusCode(500, "An error occurred while submitting feedback.");
        }
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetCustomerAnalytics([FromQuery] int restaurantId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            startDate ??= DateTime.UtcNow.AddDays(-90);
            endDate ??= DateTime.UtcNow;

            var customers = await _context.Customers
                .Where(c => c.RestaurantID == restaurantId &&
                           c.FirstVisit >= startDate)
                .ToListAsync();

            var orders = await _context.Orders
                .Where(o => o.RestaurantID == restaurantId &&
                           o.CreatedAt >= startDate &&
                           o.CreatedAt <= endDate &&
                           o.OrderStatus != OrderStatus.Cancelled)
                .ToListAsync();

            var feedback = await _context.CustomerFeedbacks
                .Where(f => f.RestaurantID == restaurantId &&
                           f.CreatedAt >= startDate &&
                           f.CreatedAt <= endDate)
                .ToListAsync();

            // ✅ ADD NULL CHECKS FOR ALL CALCULATIONS
            var analytics = new
            {
                TotalCustomers = customers?.Count ?? 0,
                NewCustomers = customers?.Count(c => c.FirstVisit >= startDate) ?? 0,
                ReturningCustomers = customers?.Count(c => c.TotalVisits > 1) ?? 0,
                AverageCustomerValue = customers?.Any() == true ? customers.Average(c => c.TotalSpent) : 0,
                CustomerSatisfaction = feedback?.Any() == true ? feedback.Average(f => f.Rating) : 0,
                RetentionRate = CalculateRetentionRate(customers ?? new List<Customer>(), startDate.Value),
                CustomerSegmentation = SegmentCustomers(customers ?? new List<Customer>())
            };

            return Ok(new { message = "Customer analytics fetched successfully", data = analytics });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching customer analytics: {ex.Message}");
            return StatusCode(500, "An error occurred while fetching customer analytics.");
        }
    }

    [HttpGet("loyalty")]
    public async Task<IActionResult> GetLoyaltyProgram([FromQuery] int restaurantId)
    {
        try
        {
            var program = await _context.LoyaltyPrograms
                .FirstOrDefaultAsync(l => l.RestaurantID == restaurantId && l.IsActive);

            var topCustomers = await _context.Customers
                .Where(c => c.RestaurantID == restaurantId)
                .OrderByDescending(c => c.LoyaltyPoints)
                .Take(10)
                .ToListAsync();

            return Ok(new
            {
                message = "Loyalty program fetched successfully",
                program = program,
                leaderboard = topCustomers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching loyalty program: {ex.Message}");
            return StatusCode(500, "An error occurred while fetching loyalty program.");
        }
    }

    [HttpPut("{id}/vip")]
    public async Task<IActionResult> ToggleVIPStatus(int id, [FromBody] bool isVIP)
    {
        try
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            customer.IsVIP = isVIP;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Customer VIP status updated to {isVIP}", data = customer });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating VIP status: {ex.Message}");
            return StatusCode(500, "An error occurred while updating VIP status.");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(int id, [FromQuery] int restaurantId)
    {
        try
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerID == id && c.RestaurantID == restaurantId);

            if (customer == null) return NotFound();

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Customer deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting customer: {ex.Message}");
            return StatusCode(500, "An error occurred while deleting customer.");
        }
    }

    // Fixed calculation methods
    private decimal CalculateCustomerLTV(Customer customer)
    {
        if (customer.TotalVisits == 0 || !customer.FirstVisit.HasValue) return 0;

        var avgOrderValue = customer.AverageOrderValue;
        var daysSinceFirstVisit = (DateTime.UtcNow - customer.FirstVisit.Value).TotalDays;

        if (daysSinceFirstVisit <= 0) return 0;

        var visitFrequency = (decimal)(customer.TotalVisits / daysSinceFirstVisit * 30); // Monthly visits
        var customerLifespan = 12; // Assume 12 months average lifespan

        return avgOrderValue * visitFrequency * customerLifespan;
    }

    private decimal CalculateVisitFrequency(Customer customer)
    {
        if (!customer.FirstVisit.HasValue || customer.TotalVisits <= 1) return 0;

        var daysSinceFirstVisit = (DateTime.UtcNow - customer.FirstVisit.Value).TotalDays;

        if (daysSinceFirstVisit <= 0) return 0;

        return (decimal)(customer.TotalVisits / daysSinceFirstVisit * 30); // Monthly frequency
    }

    private List<string> GetPreferredItems(List<Order> orders)
    {
        if (orders == null || !orders.Any())
            return new List<string>();

        return orders
            .SelectMany(o => o.OrderItems)
            .Where(oi => oi.Product != null)
            .GroupBy(oi => oi.Product.ProductName)
            .OrderByDescending(g => g.Sum(oi => oi.Quantity))
            .Take(5)
            .Select(g => g.Key)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();
    }

    private async Task UpdateCustomerStats(int customerId, int rating)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer != null)
        {
            // Update loyalty points based on rating
            customer.LoyaltyPoints += rating * 10; // 10 points per star
            await _context.SaveChangesAsync();
        }
    }

    private decimal CalculateRetentionRate(List<Customer> customers, DateTime startDate)
    {
        if (customers == null || !customers.Any())
            return 0;

        var returningCustomers = customers.Count(c => c.TotalVisits > 1 && c.LastVisit.HasValue && c.LastVisit >= startDate.AddDays(-30));
        var activeCustomers = customers.Count(c => c.LastVisit.HasValue && c.LastVisit >= startDate.AddDays(-30));

        return activeCustomers > 0 ? (decimal)returningCustomers / activeCustomers * 100 : 0;
    }

    // Fixed SegmentCustomers method - replaced 'between' and 'and' with proper C# syntax
    private object SegmentCustomers(List<Customer> customers)
    {
        if (customers == null)
            return new { VIP = 0, Regular = 0, Occasional = 0, New = 0 };

        return new
        {
            VIP = customers.Count(c => c.TotalSpent > 1000),
            Regular = customers.Count(c => c.TotalSpent >= 100 && c.TotalSpent <= 1000),
            Occasional = customers.Count(c => c.TotalSpent < 100),
            New = customers.Count(c => c.TotalVisits == 1)
        };
    }
}