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
                .FirstOrDefaultAsync(i =>
                    i.InventoryItemID == id &&
                    i.RestaurantID == restaurantId &&
                    !i.IsDeleted);
        }

        public async Task<IEnumerable<InventoryItem>> GetItemsAsync(int restaurantId, string? search = null)
        {
            var query = _context.InventoryItems
                .AsNoTracking()
                .Where(i =>
                    i.RestaurantID == restaurantId &&
                    !i.IsDeleted);

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

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException("Inventory item was modified by another process. Please refresh and retry.");
            }

            return existing;
        }

        public async Task<bool> DeleteItemAsync(int id, int restaurantId)
        {
            var item = await _context.InventoryItems
                .FirstOrDefaultAsync(i =>
                    i.InventoryItemID == id &&
                    i.RestaurantID == restaurantId);

            if (item == null) return false;

            item.IsDeleted = true;
            item.IsActive = false;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<InventoryItem>> GetLowStockItems(int restaurantId)
        {
            return await _context.InventoryItems
                .Where(i =>
                    i.RestaurantID == restaurantId &&
                    !i.IsDeleted &&
                    i.CurrentQuantity <= i.ReorderLevel)
                .ToListAsync();
        }

        // Transactions (repository should NOT create transactions - controller owns transaction boundary)
        public async Task<StockTransaction> AddTransactionAsync(StockTransaction tx)
        {
            var item = await _context.InventoryItems
                .FirstOrDefaultAsync(i =>
                    i.InventoryItemID == tx.InventoryItemID &&
                    i.RestaurantID == tx.RestaurantID &&
                    !i.IsDeleted);

            if (item == null)
                throw new KeyNotFoundException("Inventory item not found");

            var oldQty = item.CurrentQuantity;
            var oldAvg = item.AverageUnitCost;

            if (tx.QuantityChange > 0 && tx.UnitCost > 0)
            {
                var newQty = oldQty + tx.QuantityChange;
                var totalValue = (oldQty * oldAvg) + (tx.QuantityChange * tx.UnitCost);

                item.AverageUnitCost = newQty > 0
                    ? totalValue / newQty
                    : 0;

                item.CurrentQuantity = newQty;
            }
            else
            {
                item.CurrentQuantity += tx.QuantityChange;
            }

            if (item.CurrentQuantity < 0)
                throw new InvalidOperationException("Stock cannot go negative.");

            item.UpdatedAt = DateTime.UtcNow;
            item.UpdatedBy = tx.CreatedBy;

            if (tx.TransactionTime == default)
                tx.TransactionTime = DateTime.UtcNow;

            _context.StockTransactions.Add(tx);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Concurrency handled at higher level if needed
                throw new InvalidOperationException("Inventory was updated by another process. Please refresh and retry.");
            }

            return tx;
        }

        public async Task DeductInventoryForOrderAsync(
    Order order,
    string reference,
    string? createdBy = null)
        {
            if (order == null || order.OrderItems == null || !order.OrderItems.Any())
                return;

            if (string.IsNullOrWhiteSpace(reference))
                throw new ArgumentException("Reference is required.");

            // 1️⃣ Load order from DB
            var dbOrder = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderID == order.OrderID);

            if (dbOrder == null)
                throw new InvalidOperationException("Order not found.");

            var restaurantId = dbOrder.RestaurantID;

            // 2️⃣ Idempotency protection
            var alreadyDeducted = await _context.StockTransactions
                .AnyAsync(t =>
                    t.Reference == reference &&
                    t.TransactionType == StockTransactionType.Sale &&
                    t.RestaurantID == restaurantId);

            if (alreadyDeducted || dbOrder.InventoryProcessed)
                return;

            // 3️⃣ Load recipes
            var productIds = order.OrderItems
                .Select(o => o.ProductID)
                .Distinct()
                .ToList();

            var recipes = await _context.ProductRecipes
                .Where(r =>
                    productIds.Contains(r.ProductID) &&
                    r.RestaurantID == restaurantId)
                .ToListAsync();

            if (!recipes.Any())
                return;

            // 4️⃣ Calculate required quantities
            var requiredMap = new Dictionary<int, decimal>();

            foreach (var item in order.OrderItems)
            {
                var productRecipes = recipes
                    .Where(r => r.ProductID == item.ProductID);

                foreach (var recipe in productRecipes)
                {
                    var requiredQty = recipe.QuantityPerUnit * item.Quantity;

                    if (requiredMap.ContainsKey(recipe.InventoryItemID))
                        requiredMap[recipe.InventoryItemID] += requiredQty;
                    else
                        requiredMap[recipe.InventoryItemID] = requiredQty;
                }
            }

            // 5️⃣ Load inventory items
            var inventoryItems = await _context.InventoryItems
                .Where(i =>
                    requiredMap.Keys.Contains(i.InventoryItemID) &&
                    i.RestaurantID == restaurantId &&
                    !i.IsDeleted)
                .ToListAsync();

            decimal totalCOGS = 0;

            // 6️⃣ Deduct inventory
            foreach (var kv in requiredMap)
            {
                var inv = inventoryItems
                    .FirstOrDefault(i => i.InventoryItemID == kv.Key);

                if (inv == null)
                    throw new InvalidOperationException(
                        $"Inventory item missing: {kv.Key}");

                if (inv.CurrentQuantity < kv.Value)
                    throw new InvalidOperationException(
                        $"Insufficient stock for {inv.ItemName}");

                inv.CurrentQuantity -= kv.Value;
                inv.UpdatedAt = DateTime.UtcNow;
                inv.UpdatedBy = createdBy;

                totalCOGS += kv.Value * inv.AverageUnitCost;

                _context.StockTransactions.Add(new StockTransaction
                {
                    InventoryItemID = inv.InventoryItemID,
                    TransactionType = StockTransactionType.Sale,
                    QuantityChange = -kv.Value,
                    UnitCost = inv.AverageUnitCost,
                    Reference = reference,
                    Notes = $"Order #{dbOrder.OrderNumber}",
                    TransactionTime = DateTime.UtcNow,
                    RestaurantID = restaurantId,
                    CreatedBy = createdBy
                });
            }

            // 7️⃣ Mark processed
            dbOrder.CostOfGoodsSold = totalCOGS;
            dbOrder.InventoryProcessed = true;

            await _context.SaveChangesAsync();
        }

        // Reverse inventory for an order (repository must not create transactions)
        public async Task ReverseInventoryForOrderAsync(Order order)
        {
            if (order == null)
                return;

            var dbOrder = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderID == order.OrderID);

            if (dbOrder == null || !dbOrder.InventoryProcessed)
                return;

            var transactions = await _context.StockTransactions
                .Where(t =>
                    t.Reference.StartsWith($"ORDER-{order.OrderNumber}") &&
                    t.TransactionType == StockTransactionType.Sale &&
                    t.RestaurantID == dbOrder.RestaurantID)
                .ToListAsync();

            foreach (var tx in transactions)
            {
                var item = await _context.InventoryItems
                    .FirstAsync(i =>
                        i.InventoryItemID == tx.InventoryItemID &&
                        i.RestaurantID == dbOrder.RestaurantID);

                item.CurrentQuantity += -tx.QuantityChange;
                item.UpdatedAt = DateTime.UtcNow;
                item.UpdatedBy = tx.CreatedBy;

                _context.StockTransactions.Add(new StockTransaction
                {
                    InventoryItemID = item.InventoryItemID,
                    TransactionType = StockTransactionType.Return,
                    QuantityChange = -tx.QuantityChange,
                    UnitCost = tx.UnitCost,
                    Reference = $"REVERSAL-ORDER-{order.OrderNumber}",
                    Notes = "Order cancelled",
                    TransactionTime = DateTime.UtcNow,
                    RestaurantID = dbOrder.RestaurantID,
                    CreatedBy = tx.CreatedBy
                });
            }

            dbOrder.InventoryProcessed = false;
            dbOrder.CostOfGoodsSold = 0;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException("Inventory was updated by another process. Please refresh and retry.");
            }
        }

        public async Task RebuildInventoryAsync(int restaurantId)
        {
            if (restaurantId <= 0)
                throw new ArgumentException("Invalid restaurantId");

            // 1️⃣ Get all inventory items for restaurant
            var inventoryItems = await _context.InventoryItems
                .Where(i => i.RestaurantID == restaurantId && !i.IsDeleted)
                .ToListAsync();

            // 2️⃣ Reset quantities and cost
            foreach (var item in inventoryItems)
            {
                item.CurrentQuantity = 0;
                item.AverageUnitCost = 0;
                item.UpdatedAt = DateTime.UtcNow;
                item.UpdatedBy = "RebuildEngine";
            }

            await _context.SaveChangesAsync();

            // 3️⃣ Get all transactions ordered by time
            var transactions = await _context.StockTransactions
                .Where(t => t.RestaurantID == restaurantId)
                .OrderBy(t => t.TransactionTime)
                .ToListAsync();

            // 4️⃣ Replay transactions
            foreach (var tx in transactions)
            {
                var item = inventoryItems
                    .FirstOrDefault(i => i.InventoryItemID == tx.InventoryItemID);

                if (item == null)
                    continue; // ignore orphaned transaction

                var oldQty = item.CurrentQuantity;
                var oldAvg = item.AverageUnitCost;

                if (tx.QuantityChange > 0 && tx.UnitCost > 0)
                {
                    var newQty = oldQty + tx.QuantityChange;
                    var totalValue = (oldQty * oldAvg) + (tx.QuantityChange * tx.UnitCost);

                    item.AverageUnitCost = newQty > 0
                        ? totalValue / newQty
                        : 0;

                    item.CurrentQuantity = newQty;
                }
                else
                {
                    item.CurrentQuantity += tx.QuantityChange;

                    if (item.CurrentQuantity < 0)
                        item.CurrentQuantity = 0; // safety guard
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<object>> GetInventoryValuationAsync(
            int restaurantId,
            DateTime? asOfDate = null)
        {
            if (restaurantId <= 0)
                throw new ArgumentException("Invalid restaurantId");

            var cutoffDate = asOfDate ?? DateTime.UtcNow;

            var inventoryItems = await _context.InventoryItems
                .Where(i => i.RestaurantID == restaurantId && !i.IsDeleted)
                .Select(i => new
                {
                    i.InventoryItemID,
                    i.ItemName
                })
                .ToListAsync();

            var transactions = await _context.StockTransactions
                .Where(t =>
                    t.RestaurantID == restaurantId &&
                    t.TransactionTime <= cutoffDate)
                .OrderBy(t => t.TransactionTime)
                .ToListAsync();

            var result = new List<object>();

            foreach (var item in inventoryItems)
            {
                decimal quantity = 0;
                decimal avgCost = 0;

                var itemTx = transactions
                    .Where(t => t.InventoryItemID == item.InventoryItemID)
                    .ToList();

                foreach (var tx in itemTx)
                {
                    if (tx.QuantityChange > 0 && tx.UnitCost > 0)
                    {
                        var newQty = quantity + tx.QuantityChange;
                        var totalValue = (quantity * avgCost) + (tx.QuantityChange * tx.UnitCost);

                        avgCost = newQty > 0 ? totalValue / newQty : 0;
                        quantity = newQty;
                    }
                    else
                    {
                        quantity += tx.QuantityChange;

                        if (quantity < 0)
                            quantity = 0;
                    }
                }

                result.Add(new
                {
                    inventoryItemID = item.InventoryItemID,
                    itemName = item.ItemName,
                    quantity = quantity,
                    averageUnitCost = avgCost,
                    totalValue = quantity * avgCost
                });
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetWasteReportAsync(
            int restaurantId,
            DateTime? from = null,
            DateTime? to = null)
        {
            if (restaurantId <= 0)
                throw new ArgumentException("Invalid restaurantId");

            var query = _context.StockTransactions
                .Where(t =>
                    t.RestaurantID == restaurantId &&
                    t.TransactionType == StockTransactionType.Waste);

            if (from.HasValue)
                query = query.Where(t => t.TransactionTime >= from.Value);

            if (to.HasValue)
                query = query.Where(t => t.TransactionTime <= to.Value);

            var wasteTransactions = await query.ToListAsync();

            var inventoryItems = await _context.InventoryItems
                .Where(i => i.RestaurantID == restaurantId && !i.IsDeleted)
                .ToDictionaryAsync(i => i.InventoryItemID, i => i.ItemName);

            var grouped = wasteTransactions
                .GroupBy(t => t.InventoryItemID)
                .Select(g =>
                {
                    var totalQty = g.Sum(x => Math.Abs(x.QuantityChange));
                    var totalValue = g.Sum(x => Math.Abs(x.QuantityChange) * x.UnitCost);

                    return new
                    {
                        inventoryItemID = g.Key,
                        itemName = inventoryItems.ContainsKey(g.Key)
                            ? inventoryItems[g.Key]
                            : "Unknown",
                        totalWasteQuantity = totalQty,
                        totalWasteValue = totalValue
                    };
                })
                .OrderByDescending(x => x.totalWasteValue)
                .ToList();

            return grouped;
        }

        public async Task RestockInventoryAsync(
            int inventoryItemId,
            decimal quantity,
            decimal unitCost,
            int restaurantId,
            string? createdBy)
        {
            var item = await _context.InventoryItems
                .FirstOrDefaultAsync(i =>
                    i.InventoryItemID == inventoryItemId &&
                    i.RestaurantID == restaurantId &&
                    !i.IsDeleted);

            if (item == null)
                throw new InvalidOperationException("Item not found");

            var totalExistingValue = item.CurrentQuantity * item.AverageUnitCost;
            var totalIncomingValue = quantity * unitCost;
            var newQuantity = item.CurrentQuantity + quantity;

            item.CurrentQuantity = newQuantity;

            item.AverageUnitCost = newQuantity > 0
                ? (totalExistingValue + totalIncomingValue) / newQuantity
                : 0;

            item.UpdatedAt = DateTime.UtcNow;
            item.UpdatedBy = createdBy;

            _context.StockTransactions.Add(new StockTransaction
            {
                InventoryItemID = inventoryItemId,
                TransactionType = StockTransactionType.Purchase,
                QuantityChange = quantity,
                UnitCost = unitCost,
                RestaurantID = restaurantId,
                CreatedBy = createdBy,
                TransactionTime = DateTime.UtcNow,
                Reference = $"RESTOCK-{inventoryItemId}-{DateTime.UtcNow:yyyyMMddHHmmss}"
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException("Stock was modified by another process. Please retry.");
            }
        }

        public async Task<IEnumerable<StockTransaction>> GetTransactionsAsync(int restaurantId, int? itemId = null, DateTime? from = null, DateTime? to = null)
        {
            var q = _context.StockTransactions.AsNoTracking().Where(x => x.RestaurantID == restaurantId);
            if (itemId.HasValue) q = q.Where(x => x.InventoryItemID == itemId.Value);
            if (from.HasValue) q = q.Where(x => x.TransactionTime >= from.Value);
            if (to.HasValue) q = q.Where(x => x.TransactionTime <= to.Value);
            return await q.OrderByDescending(x => x.TransactionTime).ToListAsync();
        }

        private async Task<decimal> ConvertToBaseUnitAsync(
            int inventoryItemId,
            decimal quantity,
            string unit,
            int restaurantId)
        {
            var item = await _context.InventoryItems
                .FirstAsync(i => i.InventoryItemID == inventoryItemId &&
                                 i.RestaurantID == restaurantId);

            if (unit == item.BaseUnit)
                return quantity;

            var conversion = await _context.UnitConversions
                .FirstOrDefaultAsync(u =>
                    u.InventoryItemID == inventoryItemId &&
                    u.FromUnit == unit &&
                    u.ToUnit == item.BaseUnit &&
                    u.RestaurantID == restaurantId);

            if (conversion == null)
                throw new InvalidOperationException(
                    $"Conversion from {unit} to {item.BaseUnit} not defined.");

            return quantity * conversion.ConversionFactor;
        }

        public async Task<StockAudit> PerformAuditAsync(
            int inventoryItemId,
            decimal physicalQuantity,
            int restaurantId,
            string? createdBy)
        {
            var item = await _context.InventoryItems
                .FirstAsync(i => i.InventoryItemID == inventoryItemId &&
                                 i.RestaurantID == restaurantId);

            var audit = new StockAudit
            {
                InventoryItemID = inventoryItemId,
                SystemQuantity = item.CurrentQuantity,
                PhysicalQuantity = physicalQuantity,
                RestaurantID = restaurantId
            };

            _context.StockAudits.Add(audit);

            var variance = physicalQuantity - item.CurrentQuantity;

            if (variance != 0)
            {
                item.CurrentQuantity = physicalQuantity;

                _context.StockTransactions.Add(new StockTransaction
                {
                    InventoryItemID = inventoryItemId,
                    TransactionType = StockTransactionType.Adjustment,
                    QuantityChange = variance,
                    UnitCost = item.AverageUnitCost,
                    Reference = $"AUDIT-{DateTime.UtcNow:yyyyMMdd}",
                    RestaurantID = restaurantId,
                    CreatedBy = createdBy
                });
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException("Stock was modified by another process. Please retry.");
            }

            return audit;
        }

        public async Task<IEnumerable<object>> GetVarianceReportAsync(int restaurantId)
        {
            return await _context.StockAudits
                .Where(a => a.RestaurantID == restaurantId)
                .Select(a => new
                {
                    a.InventoryItemID,
                    a.SystemQuantity,
                    a.PhysicalQuantity,
                    Variance = a.PhysicalQuantity - a.SystemQuantity,
                    a.AuditDate
                })
                .OrderByDescending(a => a.AuditDate)
                .ToListAsync();
        }

        public async Task<object> GetInventoryTurnoverAsync(int restaurantId)
        {
            var cogs = await _context.Orders
                .Where(o => o.RestaurantID == restaurantId &&
                            o.InventoryProcessed)
                .SumAsync(o => o.CostOfGoodsSold);

            var avgInventory = await _context.InventoryItems
                .Where(i => i.RestaurantID == restaurantId)
                .AverageAsync(i => i.CurrentQuantity * i.AverageUnitCost);

            return new
            {
                TotalCOGS = cogs,
                AverageInventoryValue = avgInventory,
                TurnoverRatio = avgInventory == 0 ? 0 : cogs / avgInventory
            };
        }

        public async Task<IEnumerable<object>> GetDeadStockAsync(
            int restaurantId,
            int days = 30)
        {
            var cutoff = DateTime.UtcNow.AddDays(-days);

            return await _context.InventoryItems
                .Where(i => i.RestaurantID == restaurantId &&
                            !_context.StockTransactions
                                .Any(t => t.InventoryItemID == i.InventoryItemID &&
                                          t.TransactionTime >= cutoff))
                .Select(i => new
                {
                    i.InventoryItemID,
                    i.ItemName,
                    i.CurrentQuantity,
                    StockValue = i.CurrentQuantity * i.AverageUnitCost
                })
                .ToListAsync();
        }

        public async Task<object> GetWasteAnalyticsAsync(int restaurantId)
        {
            var waste = await _context.StockTransactions
                .Where(t => t.RestaurantID == restaurantId &&
                            t.TransactionType == StockTransactionType.Waste)
                .SumAsync(t => Math.Abs(t.QuantityChange) * t.UnitCost);

            var purchase = await _context.StockTransactions
                .Where(t => t.RestaurantID == restaurantId &&
                            t.TransactionType == StockTransactionType.Purchase)
                .SumAsync(t => t.QuantityChange * t.UnitCost);

            return new
            {
                WasteValue = waste,
                PurchaseValue = purchase,
                WastePercentage = purchase == 0 ? 0 : (waste / purchase) * 100
            };
        }

        public async Task<UnitConversion> AddOrUpdateConversionAsync(UnitConversion conversion)
        {
            var existing = await _context.UnitConversions
                .FirstOrDefaultAsync(u =>
                    u.InventoryItemID == conversion.InventoryItemID &&
                    u.FromUnit == conversion.FromUnit &&
                    u.ToUnit == conversion.ToUnit &&
                    u.RestaurantID == conversion.RestaurantID);

            if (existing == null)
            {
                _context.UnitConversions.Add(conversion);
            }
            else
            {
                existing.ConversionFactor = conversion.ConversionFactor;
            }

            await _context.SaveChangesAsync();
            return existing ?? conversion;
        }

        public async Task<bool> DeleteConversionAsync(int id, int restaurantId)
        {
            var existing = await _context.UnitConversions
                .FirstOrDefaultAsync(u =>
                    u.UnitConversionID == id &&
                    u.RestaurantID == restaurantId);

            if (existing == null) return false;

            _context.UnitConversions.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<UnitConversion>> GetConversionsAsync(
            int inventoryItemId,
            int restaurantId)
        {
            return await _context.UnitConversions
                .Where(u =>
                    u.InventoryItemID == inventoryItemId &&
                    u.RestaurantID == restaurantId)
                .ToListAsync();
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