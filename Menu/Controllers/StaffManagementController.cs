using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Models;
using Restaurant_System.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class StaffManagementController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StaffManagementController> _logger;

    public StaffManagementController(ApplicationDbContext context, ILogger<StaffManagementController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("staff")]
    public async Task<IActionResult> GetStaff([FromQuery] int restaurantId)
    {
        try
        {
            var staff = await _context.Staff
                .Where(s => s.RestaurantID == restaurantId)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return Ok(new { message = "Staff fetched successfully", data = staff });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching staff: {ex.Message}");
            return StatusCode(500, "An error occurred while fetching staff data.");
        }
    }

    [HttpPost("staff")]
    public async Task<IActionResult> AddStaff([FromBody] Staff staff)
    {
        try
        {
            staff.HireDate = DateTime.UtcNow;
            _context.Staff.Add(staff);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Staff added successfully", data = staff });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding staff: {ex.Message}");
            return StatusCode(500, "An error occurred while adding staff.");
        }
    }

    [HttpPut("staff/{id}")]
    public async Task<IActionResult> UpdateStaff(int id, [FromBody] Staff staffUpdate)
    {
        try
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff == null) return NotFound();

            staff.Name = staffUpdate.Name;
            staff.Role = staffUpdate.Role;
            staff.Phone = staffUpdate.Phone;
            staff.Email = staffUpdate.Email;
            staff.HourlyRate = staffUpdate.HourlyRate;
            staff.IsActive = staffUpdate.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Staff updated successfully", data = staff });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating staff: {ex.Message}");
            return StatusCode(500, "An error occurred while updating staff.");
        }
    }

    [HttpGet("shifts")]
    public async Task<IActionResult> GetShifts([FromQuery] int restaurantId, [FromQuery] DateTime? date = null)
    {
        try
        {
            var query = _context.StaffShifts
                .Include(s => s.Staff)
                .Where(s => s.RestaurantID == restaurantId);

            if (date.HasValue)
            {
                query = query.Where(s => s.ShiftDate.Date == date.Value.Date);
            }

            var shifts = await query.OrderBy(s => s.ShiftDate).ThenBy(s => s.StartTime).ToListAsync();

            return Ok(new { message = "Shifts fetched successfully", data = shifts });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching shifts: {ex.Message}");
            return StatusCode(500, "An error occurred while fetching shifts.");
        }
    }

    [HttpPost("shifts")]
    public async Task<IActionResult> CreateShift([FromBody] StaffShift shift)
    {
        try
        {
            // Calculate hours worked
            var start = DateTime.Today.Add(shift.StartTime);
            var end = DateTime.Today.Add(shift.EndTime);
            shift.HoursWorked = (decimal)(end - start).TotalHours;

            _context.StaffShifts.Add(shift);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Shift created successfully", data = shift });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating shift: {ex.Message}");
            return StatusCode(500, "An error occurred while creating shift.");
        }
    }

    [HttpGet("performance")]
    public async Task<IActionResult> GetStaffPerformance([FromQuery] int restaurantId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var performance = await _context.StaffPerformances
                .Include(p => p.Staff)
                .Where(p => p.RestaurantID == restaurantId &&
                           p.PerformanceDate >= startDate &&
                           p.PerformanceDate <= endDate)
                .OrderByDescending(p => p.PerformanceDate)
                .ToListAsync();

            // Calculate leaderboard
            var leaderboard = performance
                .GroupBy(p => p.StaffID)
                .Select(g => new
                {
                    StaffID = g.Key,
                    StaffName = g.First().Staff.Name,
                    TotalOrders = g.Sum(p => p.OrdersServed),
                    TotalSales = g.Sum(p => p.TotalSales),
                    AvgEfficiency = g.Average(p => p.EfficiencyScore)
                })
                .OrderByDescending(x => x.TotalSales)
                .ToList();

            return Ok(new
            {
                message = "Staff performance fetched successfully",
                performance = performance,
                leaderboard = leaderboard
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching staff performance: {ex.Message}");
            return StatusCode(500, "An error occurred while fetching staff performance.");
        }
    }
}