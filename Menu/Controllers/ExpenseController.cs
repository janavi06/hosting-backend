using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Models;
using Restaurant_System.Models;

namespace Restaurant_Menu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExpenseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryRepository _inventory;

        public ExpenseController(ApplicationDbContext context, IInventoryRepository inventory)
        {
            _context = context;
            _inventory = inventory;
        }

        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] Expense expense)
        {
            if (expense.RestaurantID <= 0)
                return BadRequest("restaurantId required");

            bool isInventoryExpense = expense.InventoryItemID.HasValue;

            // 🔥 VALIDATION
            if (isInventoryExpense)
            {
                if (!expense.Quantity.HasValue || !expense.UnitCost.HasValue)
                    return BadRequest("Quantity and UnitCost required for inventory expense");

                if (expense.Quantity <= 0 || expense.UnitCost <= 0)
                    return BadRequest("Invalid quantity or cost");

                expense.TotalCost = expense.Quantity.Value * expense.UnitCost.Value;
                expense.Amount = expense.TotalCost.Value;
            }

            expense.CreatedAt = DateTime.UtcNow;
            expense.UpdatedAt = DateTime.UtcNow;

            _context.Expenses.Add(expense);

            // 🔥 INVENTORY SYNC
            if (isInventoryExpense)
            {
                await _inventory.AddTransactionAsync(new StockTransaction
                {
                    InventoryItemID = expense.InventoryItemID!.Value,
                    TransactionType = StockTransactionType.Purchase,
                    QuantityChange = expense.Quantity!.Value,
                    UnitCost = expense.UnitCost!.Value,
                    Reference = $"EXP-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    RestaurantID = expense.RestaurantID,
                    CreatedBy = User.Identity?.Name ?? "System"
                });
            }

            await _context.SaveChangesAsync();

            return Ok(expense);
        }

        [HttpGet]
        public async Task<IActionResult> GetExpenses([FromQuery] int restaurantId)
        {
            if (restaurantId <= 0)
                return BadRequest("restaurantId required");

            var data = await _context.Expenses
                .Where(e => e.RestaurantID == restaurantId)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();

            return Ok(data);
        }
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(int restaurantId)
        {
            var totalExpense = await _context.Expenses
                .Where(e => e.RestaurantID == restaurantId)
                .SumAsync(e => e.Amount);

            var foodExpense = await _context.Expenses
                .Where(e => e.RestaurantID == restaurantId && e.Category == ExpenseCategory.Food)
                .SumAsync(e => e.Amount);

            return Ok(new
            {
                totalExpense,
                foodExpense
            });
        }
    }
} 