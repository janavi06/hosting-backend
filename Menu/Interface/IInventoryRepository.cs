using Restaurant_Menu.Models;

namespace Restaurant_Menu.Interface
{
    public interface IInventoryRepository
    {
        // Inventory Items
        Task<InventoryItem?> GetItemAsync(int id, int restaurantId);
        Task<IEnumerable<InventoryItem>> GetItemsAsync(int restaurantId, string? search = null);
        Task<InventoryItem> CreateItemAsync(InventoryItem item);
        Task<InventoryItem?> UpdateItemAsync(InventoryItem item);
        Task<bool> DeleteItemAsync(int id, int restaurantId);

        // Stock Transactions
        Task<StockTransaction> AddTransactionAsync(StockTransaction tx);
        Task<IEnumerable<StockTransaction>> GetTransactionsAsync(int restaurantId, int? itemId = null, DateTime? from = null, DateTime? to = null);

        // Recipe
        Task<IEnumerable<ProductRecipe>> GetProductRecipeAsync(int productId, int restaurantId);
        Task<ProductRecipe> UpsertProductRecipeAsync(ProductRecipe recipe);
        Task<bool> RemoveProductRecipeAsync(int productRecipeId, int restaurantId);
    }
}
