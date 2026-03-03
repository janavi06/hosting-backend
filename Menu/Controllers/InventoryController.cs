using Microsoft.AspNetCore.Mvc;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Models;

namespace Restaurant_Menu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryRepository _inventory;
        public InventoryController(IInventoryRepository inventory)
        {
            _inventory = inventory;
        }

        // Items
        [HttpGet("items")]
        public async Task<ActionResult<IEnumerable<InventoryItem>>> GetItems([FromQuery] int restaurantId, [FromQuery] string? search)
        {
            if (restaurantId <= 0) return BadRequest("restaurantId is required");
            var items = await _inventory.GetItemsAsync(restaurantId, search);
            return Ok(items);
        }

        [HttpGet("items/{id:int}")]
        public async Task<ActionResult<InventoryItem>> GetItem([FromRoute] int id, [FromQuery] int restaurantId)
        {
            if (restaurantId <= 0) return BadRequest("restaurantId is required");
            var item = await _inventory.GetItemAsync(id, restaurantId);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost("items")]
        public async Task<ActionResult<InventoryItem>> CreateItem([FromBody] InventoryItem item)
        {
            if (item.RestaurantID <= 0) return BadRequest("restaurantId is required");
            var created = await _inventory.CreateItemAsync(item);
            return CreatedAtAction(nameof(GetItem), new { id = created.InventoryItemID, restaurantId = created.RestaurantID }, created);
        }

        [HttpPut("items/{id:int}")]
        public async Task<ActionResult<InventoryItem>> UpdateItem([FromRoute] int id, [FromBody] InventoryItem item)
        {
            if (id != item.InventoryItemID) return BadRequest("Mismatched id");
            if (item.RestaurantID <= 0) return BadRequest("restaurantId is required");
            var updated = await _inventory.UpdateItemAsync(item);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("items/{id:int}")]
        public async Task<IActionResult> DeleteItem([FromRoute] int id, [FromQuery] int restaurantId)
        {
            if (restaurantId <= 0) return BadRequest("restaurantId is required");
            var ok = await _inventory.DeleteItemAsync(id, restaurantId);
            if (!ok) return NotFound();
            return NoContent();
        }

        // Transactions
        [HttpGet("transactions")]
        public async Task<ActionResult<IEnumerable<StockTransaction>>> GetTransactions([FromQuery] int restaurantId, [FromQuery] int? itemId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            if (restaurantId <= 0) return BadRequest("restaurantId is required");
            var list = await _inventory.GetTransactionsAsync(restaurantId, itemId, from, to);
            return Ok(list);
        }

        [HttpPost("transactions")]
        public async Task<ActionResult<StockTransaction>> AddTransaction([FromBody] StockTransaction tx)
        {
            if (tx.RestaurantID <= 0) return BadRequest("restaurantId is required");
            if (tx.InventoryItemID <= 0) return BadRequest("inventoryItemId is required");
            var created = await _inventory.AddTransactionAsync(tx);
            return CreatedAtAction(nameof(GetTransactions), new { restaurantId = created.RestaurantID, itemId = created.InventoryItemID }, created);
        }

        // Recipes
        [HttpGet("recipes/{productId:int}")]
        public async Task<ActionResult<IEnumerable<ProductRecipe>>> GetRecipe([FromRoute] int productId, [FromQuery] int restaurantId)
        {
            if (restaurantId <= 0) return BadRequest("restaurantId is required");
            var list = await _inventory.GetProductRecipeAsync(productId, restaurantId);
            return Ok(list);
        }

        [HttpPost("recipes")]
        public async Task<ActionResult<ProductRecipe>> UpsertRecipe([FromBody] ProductRecipe recipe)
        {
            if (recipe.RestaurantID <= 0) return BadRequest("restaurantId is required");
            var saved = await _inventory.UpsertProductRecipeAsync(recipe);
            return Ok(saved);
        }
        [HttpGet("analytics/turnover")]
        public async Task<IActionResult> GetTurnover(
    [FromQuery] int restaurantId)
        {
            if (restaurantId <= 0)
                return BadRequest("restaurantId required");

            var result = await _inventory
                .GetInventoryTurnoverAsync(restaurantId);

            return Ok(result);
        }

        [HttpGet("analytics/dead-stock")]
        public async Task<IActionResult> GetDeadStock(
            [FromQuery] int restaurantId,
            [FromQuery] int days = 30)
        {
            if (restaurantId <= 0)
                return BadRequest("restaurantId required");

            var result = await _inventory
                .GetDeadStockAsync(restaurantId, days);

            return Ok(result);
        }

        [HttpGet("analytics/waste")]
        public async Task<IActionResult> GetWasteAnalytics(
            [FromQuery] int restaurantId)
        {
            if (restaurantId <= 0)
                return BadRequest("restaurantId required");

            var result = await _inventory
                .GetWasteAnalyticsAsync(restaurantId);

            return Ok(result);
        }
        [HttpPost("audit")]
        public async Task<IActionResult> PerformAudit(
    [FromQuery] int inventoryItemId,
    [FromQuery] decimal physicalQuantity,
    [FromQuery] int restaurantId)
        {
            if (restaurantId <= 0)
                return BadRequest("restaurantId required");

            var result = await _inventory.PerformAuditAsync(
                inventoryItemId,
                physicalQuantity,
                restaurantId,
                User.Identity?.Name ?? "System");

            return Ok(result);
        }

        [HttpGet("variance-report")]
        public async Task<IActionResult> GetVarianceReport(
            [FromQuery] int restaurantId)
        {
            if (restaurantId <= 0)
                return BadRequest("restaurantId required");

            var result = await _inventory
                .GetVarianceReportAsync(restaurantId);

            return Ok(result);
        }
        [HttpGet("conversions/{inventoryItemId:int}")]
        public async Task<IActionResult> GetConversions(
    int inventoryItemId,
    [FromQuery] int restaurantId)
        {
            if (restaurantId <= 0)
                return BadRequest("restaurantId required");

            var result = await _inventory.GetConversionsAsync(
                inventoryItemId,
                restaurantId);

            return Ok(result);
        }

        [HttpPost("conversions")]
        public async Task<IActionResult> AddOrUpdateConversion(
            [FromBody] UnitConversion conversion)
        {
            if (conversion.RestaurantID <= 0)
                return BadRequest("restaurantId required");

            var result = await _inventory
                .AddOrUpdateConversionAsync(conversion);

            return Ok(result);
        }

        [HttpDelete("conversions/{id:int}")]
        public async Task<IActionResult> DeleteConversion(
            int id,
            [FromQuery] int restaurantId)
        {
            var ok = await _inventory
                .DeleteConversionAsync(id, restaurantId);

            if (!ok) return NotFound();

            return NoContent();
        }

        [HttpGet("valuation")]
        public async Task<IActionResult> GetValuation(
    [FromQuery] int restaurantId,
    [FromQuery] DateTime? asOfDate)
        {
            if (restaurantId <= 0)
                return BadRequest("restaurantId is required");

            var result = await _inventory.GetInventoryValuationAsync(restaurantId, asOfDate);
            return Ok(result);
        }
        [HttpPost("rebuild")]
        public async Task<IActionResult> RebuildInventory([FromQuery] int restaurantId)
        {
            if (restaurantId <= 0)
                return BadRequest("restaurantId is required");

            await _inventory.RebuildInventoryAsync(restaurantId);
            return Ok("Inventory rebuilt successfully.");
        }
        [HttpGet("waste-report")]
        public async Task<IActionResult> GetWasteReport(
    [FromQuery] int restaurantId,
    [FromQuery] DateTime? from,
    [FromQuery] DateTime? to)
        {
            if (restaurantId <= 0)
                return BadRequest("restaurantId is required");

            var result = await _inventory.GetWasteReportAsync(restaurantId, from, to);
            return Ok(result);
        }
        [HttpDelete("recipes/{productRecipeId:int}")]
        public async Task<IActionResult> DeleteRecipe([FromRoute] int productRecipeId, [FromQuery] int restaurantId)
        {
            if (restaurantId <= 0) return BadRequest("restaurantId is required");
            var ok = await _inventory.RemoveProductRecipeAsync(productRecipeId, restaurantId);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
