using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Models;

namespace Restaurant_Menu.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly ApplicationDbContext _context;

        public InventoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Inventory Items
        public async Task<InventoryItem?> GetItemAsync(int id, int restaurantId)
        {
            return await _context.InventoryItems
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InventoryItemID == id && i.RestaurantID == restaurantId);
        }

        public async Task<IEnumerable<InventoryItem>> GetItemsAsync(int restaurantId, string? search = null)
        {
            var query = _context.InventoryItems.AsNoTracking().Where(i => i.RestaurantID == restaurantId);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(i => i.ItemName.ToLower().Contains(s) || (i.SKU != null && i.SKU.ToLower().Contains(s)));
            }
            return await query.OrderBy(i => i.ItemName).ToListAsync();
        }

        public async Task<InventoryItem> CreateItemAsync(InventoryItem item)
        {
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            _context.InventoryItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<InventoryItem?> UpdateItemAsync(InventoryItem item)
        {
            var existing = await _context.InventoryItems.FirstOrDefaultAsync(i => i.InventoryItemID == item.InventoryItemID && i.RestaurantID == item.RestaurantID);
            if (existing == null) return null;

            existing.ItemName = item.ItemName;
            existing.SKU = item.SKU;
            existing.UnitOfMeasure = item.UnitOfMeasure;
            existing.ReorderLevel = item.ReorderLevel;
            existing.IsActive = item.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = item.UpdatedBy;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteItemAsync(int id, int restaurantId)
        {
            var existing = await _context.InventoryItems.FirstOrDefaultAsync(i => i.InventoryItemID == id && i.RestaurantID == restaurantId);
            if (existing == null) return false;
            _context.InventoryItems.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        // Transactions
        public async Task<StockTransaction> AddTransactionAsync(StockTransaction tx)
        {
            using var t = await _context.Database.BeginTransactionAsync();

            var item = await _context.InventoryItems.FirstOrDefaultAsync(i => i.InventoryItemID == tx.InventoryItemID && i.RestaurantID == tx.RestaurantID);
            if (item == null) throw new KeyNotFoundException("Inventory item not found");

            // Adjust quantity
            item.CurrentQuantity += tx.QuantityChange;
            item.UpdatedAt = DateTime.UtcNow;
            item.UpdatedBy = tx.CreatedBy;

            // Average cost update on positive quantities (e.g., purchases/returns)
            if (tx.QuantityChange > 0 && tx.UnitCost > 0)
            {
                // Simple moving average
                var currentValue = item.CurrentQuantity * item.AverageUnitCost;
                var addValue = tx.QuantityChange * tx.UnitCost;
                var newQty = item.CurrentQuantity;
                item.AverageUnitCost = newQty > 0 ? (currentValue + addValue) / newQty : item.AverageUnitCost;
            }

            tx.TransactionTime = DateTime.UtcNow;

            _context.StockTransactions.Add(tx);
            await _context.SaveChangesAsync();
            await t.CommitAsync();
            return tx;
        }

        public async Task<IEnumerable<StockTransaction>> GetTransactionsAsync(int restaurantId, int? itemId = null, DateTime? from = null, DateTime? to = null)
        {
            var q = _context.StockTransactions.AsNoTracking().Where(x => x.RestaurantID == restaurantId);
            if (itemId.HasValue) q = q.Where(x => x.InventoryItemID == itemId.Value);
            if (from.HasValue) q = q.Where(x => x.TransactionTime >= from.Value);
            if (to.HasValue) q = q.Where(x => x.TransactionTime <= to.Value);
            return await q.OrderByDescending(x => x.TransactionTime).ToListAsync();
        }

        // Recipe
        public async Task<IEnumerable<ProductRecipe>> GetProductRecipeAsync(int productId, int restaurantId)
        {
            return await _context.ProductRecipes
                .AsNoTracking()
                .Where(r => r.ProductID == productId && r.RestaurantID == restaurantId)
                .ToListAsync();
        }

        public async Task<ProductRecipe> UpsertProductRecipeAsync(ProductRecipe recipe)
        {
            var existing = await _context.ProductRecipes.FirstOrDefaultAsync(r => r.ProductID == recipe.ProductID && r.InventoryItemID == recipe.InventoryItemID && r.RestaurantID == recipe.RestaurantID);
            if (existing == null)
            {
                _context.ProductRecipes.Add(recipe);
            }
            else
            {
                existing.QuantityPerUnit = recipe.QuantityPerUnit;
            }
            await _context.SaveChangesAsync();
            return existing ?? recipe;
        }

        public async Task<bool> RemoveProductRecipeAsync(int productRecipeId, int restaurantId)
        {
            var existing = await _context.ProductRecipes.FirstOrDefaultAsync(r => r.ProductRecipeID == productRecipeId && r.RestaurantID == restaurantId);
            if (existing == null) return false;
            _context.ProductRecipes.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
