using Restaurant_Menu.Models;

namespace Restaurant_Menu.Interface
{
    public interface IInventoryRepository
    {
        // =============================
        // Inventory Items
        // =============================

        Task<InventoryItem?> GetItemAsync(
            int id,
            int restaurantId);

        Task<IEnumerable<InventoryItem>> GetItemsAsync(
            int restaurantId,
            string? search = null);

        Task<InventoryItem> CreateItemAsync(
            InventoryItem item);

        Task<InventoryItem?> UpdateItemAsync(
            InventoryItem item);

        Task<bool> DeleteItemAsync(
            int id,
            int restaurantId);


        // =============================
        // Stock Transactions
        // =============================

        Task<StockTransaction> AddTransactionAsync(
            StockTransaction tx);
        Task<StockAudit> PerformAuditAsync(
    int inventoryItemId,
    decimal physicalQuantity,
    int restaurantId,
    string? createdBy);

        Task<IEnumerable<object>> GetVarianceReportAsync(int restaurantId);
        Task<object> GetInventoryTurnoverAsync(int restaurantId);
        Task<IEnumerable<object>> GetDeadStockAsync(int restaurantId, int days);
        Task<object> GetWasteAnalyticsAsync(int restaurantId);
        Task<IEnumerable<StockTransaction>> GetTransactionsAsync(
            int restaurantId,
            int? itemId = null,
            DateTime? from = null,
            DateTime? to = null);
        Task<UnitConversion> AddOrUpdateConversionAsync(UnitConversion conversion);
        Task<bool> DeleteConversionAsync(int id, int restaurantId);
        Task<IEnumerable<UnitConversion>> GetConversionsAsync(int inventoryItemId, int restaurantId);



        // =============================
        // Order Inventory Processing
        // =============================

        Task DeductInventoryForOrderAsync(
            Order order,
            string reference,
            string? createdBy = null);

        Task ReverseInventoryForOrderAsync(
            Order order);
        Task<IEnumerable<object>> GetWasteReportAsync(
    int restaurantId,
    DateTime? from = null,
    DateTime? to = null);

        // =============================
        // Product Recipes
        // =============================

        Task<IEnumerable<ProductRecipe>> GetProductRecipeAsync(
            int productId,
            int restaurantId);
        Task<IEnumerable<object>> GetInventoryValuationAsync(
    int restaurantId,
    DateTime? asOfDate = null);
        Task<ProductRecipe> UpsertProductRecipeAsync(
            ProductRecipe recipe);
        Task RebuildInventoryAsync(int restaurantId);
        Task<bool> RemoveProductRecipeAsync(
            int productRecipeId,
            int restaurantId);
    }
}