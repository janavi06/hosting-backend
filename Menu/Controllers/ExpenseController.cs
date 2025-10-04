using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_System.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class ExpenseController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExpenseController> _logger;

    public ExpenseController(ApplicationDbContext context, ILogger<ExpenseController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetExpenses([FromQuery] int restaurantId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var expenses = await _context.Expenses
                .Where(e => e.RestaurantID == restaurantId &&
                           e.ExpenseDate >= startDate &&
                           e.ExpenseDate <= endDate)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();

            var summary = expenses
                .GroupBy(e => e.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    TotalAmount = g.Sum(e => e.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            return Ok(new
            {
                message = "Expenses fetched successfully",
                expenses = expenses,
                summary = summary,
                totalExpenses = expenses.Sum(e => e.Amount)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching expenses: {ex.Message}");
            return StatusCode(500, "An error occurred while fetching expenses.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateExpense([FromBody] Expense expense)
    {
        try
        {
            expense.CreatedAt = DateTime.UtcNow;
            expense.UpdatedAt = DateTime.UtcNow;

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            // Update budget if exists
            await UpdateBudget(expense.RestaurantID, expense.Category, expense.Amount, expense.ExpenseDate);

            return Ok(new { message = "Expense added successfully", data = expense });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating expense: {ex.Message}");
            return StatusCode(500, "An error occurred while creating expense.");
        }
    }

    [HttpGet("budgets")]
    public async Task<IActionResult> GetBudgets([FromQuery] int restaurantId, [FromQuery] int year, [FromQuery] int month)
    {
        try
        {
            var budgets = await _context.Budgets
                .Where(b => b.RestaurantID == restaurantId && b.Year == year && b.Month == month)
                .ToListAsync();

            // ✅ ADD NULL CHECK
            if (budgets == null || !budgets.Any())
            {
                return Ok(new
                {
                    message = "No budget data found for specified period",
                    data = new List<Budget>()
                });
            }

            // Calculate actual spent for each category
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var expenses = await _context.Expenses
                .Where(e => e.RestaurantID == restaurantId &&
                           e.ExpenseDate >= startDate &&
                           e.ExpenseDate <= endDate)
                .GroupBy(e => e.Category)
                .Select(g => new { Category = g.Key, ActualSpent = g.Sum(e => e.Amount) })
                .ToListAsync();

            foreach (var budget in budgets)
            {
                var expense = expenses.FirstOrDefault(e => e.Category == budget.Category);
                budget.ActualSpent = expense?.ActualSpent ?? 0;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Budgets fetched successfully", data = budgets });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching budgets: {ex.Message}");
            return StatusCode(500, "An error occurred while fetching budgets.");
        }
    }

    [HttpPost("budgets")]
    public async Task<IActionResult> CreateBudget([FromBody] Budget budget)
    {
        try
        {
            var existing = await _context.Budgets
                .FirstOrDefaultAsync(b => b.RestaurantID == budget.RestaurantID &&
                                         b.Category == budget.Category &&
                                         b.Year == budget.Year &&
                                         b.Month == budget.Month);

            if (existing != null)
            {
                existing.MonthlyBudget = budget.MonthlyBudget;
            }
            else
            {
                _context.Budgets.Add(budget);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Budget saved successfully", data = budget });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating budget: {ex.Message}");
            return StatusCode(500, "An error occurred while creating budget.");
        }
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetExpenseReports([FromQuery] int restaurantId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var expenses = await _context.Expenses
                .Where(e => e.RestaurantID == restaurantId &&
                           e.ExpenseDate >= startDate &&
                           e.ExpenseDate <= endDate)
                .ToListAsync();

            var totalExpenses = expenses.Sum(e => e.Amount);

            var categoryBreakdown = expenses
                .GroupBy(e => e.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    TotalAmount = g.Sum(e => e.Amount),
                    Percentage = totalExpenses > 0 ?
                        (g.Sum(e => e.Amount) / totalExpenses) * 100 : 0
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            var monthlyTrend = expenses
                .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
                .Select(g => new
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    TotalExpenses = g.Sum(e => e.Amount),
                    CategoryBreakdown = g.GroupBy(e => e.Category)
                        .Select(cg => new { Category = cg.Key, Amount = cg.Sum(e => e.Amount) })
                })
                .OrderBy(x => x.Period)
                .ToList();

            // Fixed division: Convert TimeSpan.TotalDays to decimal before division
            var daysDifference = (decimal)(endDate - startDate).TotalDays;
            var averageMonthlyExpense = expenses.Any() && daysDifference > 0 ?
                totalExpenses / (daysDifference / 30) : 0;

            return Ok(new
            {
                message = "Expense report generated successfully",
                summary = new
                {
                    TotalExpenses = totalExpenses,
                    AverageMonthlyExpense = averageMonthlyExpense,
                    MostExpensiveCategory = categoryBreakdown.FirstOrDefault(),
                    TotalTransactions = expenses.Count
                },
                categoryBreakdown,
                monthlyTrend
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating expense report: {ex.Message}");
            return StatusCode(500, "An error occurred while generating expense report.");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExpense(int id, [FromBody] Expense expenseUpdate)
    {
        try
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null) return NotFound();

            // Store old values for budget update
            var oldAmount = expense.Amount;
            var oldCategory = expense.Category;
            var oldDate = expense.ExpenseDate;

            // Update expense
            expense.Category = expenseUpdate.Category;
            expense.Description = expenseUpdate.Description;
            expense.Amount = expenseUpdate.Amount;
            expense.ExpenseDate = expenseUpdate.ExpenseDate;
            expense.PaymentMethod = expenseUpdate.PaymentMethod;
            expense.Vendor = expenseUpdate.Vendor;
            expense.ReceiptNumber = expenseUpdate.ReceiptNumber;
            expense.Notes = expenseUpdate.Notes;
            expense.IsRecurring = expenseUpdate.IsRecurring;
            expense.RecurringFrequency = expenseUpdate.RecurringFrequency;
            expense.ApprovedBy = expenseUpdate.ApprovedBy;
            expense.UpdatedAt = DateTime.UtcNow;

            // Update budgets - remove old amount and add new amount
            if (oldAmount != expenseUpdate.Amount || oldCategory != expenseUpdate.Category || oldDate != expenseUpdate.ExpenseDate)
            {
                // Remove old amount from old budget period
                await UpdateBudget(expense.RestaurantID, oldCategory, -oldAmount, oldDate);

                // Add new amount to new budget period
                await UpdateBudget(expense.RestaurantID, expenseUpdate.Category, expenseUpdate.Amount, expenseUpdate.ExpenseDate);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Expense updated successfully", data = expense });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating expense: {ex.Message}");
            return StatusCode(500, "An error occurred while updating expense.");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(int id, [FromQuery] int restaurantId)
    {
        try
        {
            var expense = await _context.Expenses
                .FirstOrDefaultAsync(e => e.ExpenseID == id && e.RestaurantID == restaurantId);

            if (expense == null) return NotFound();

            // Remove amount from budget before deleting
            await UpdateBudget(expense.RestaurantID, expense.Category, -expense.Amount, expense.ExpenseDate);

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Expense deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting expense: {ex.Message}");
            return StatusCode(500, "An error occurred while deleting expense.");
        }
    }

    private async Task UpdateBudget(int restaurantId, ExpenseCategory category, decimal amount, DateTime expenseDate)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.RestaurantID == restaurantId &&
                                     b.Category == category &&
                                     b.Year == expenseDate.Year &&
                                     b.Month == expenseDate.Month);

        if (budget != null)
        {
            budget.ActualSpent += amount;

            // Ensure ActualSpent doesn't go negative
            if (budget.ActualSpent < 0)
                budget.ActualSpent = 0;

            await _context.SaveChangesAsync();
        }
    }
}