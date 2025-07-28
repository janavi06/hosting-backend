
using Restaurant_Menu.Models;

public interface IRestaurantTableRepository
{
    Task<IEnumerable<RestaurantTable>> GetAllTablesAsync();
    Task<RestaurantTable> GetTableByIdAsync(int tableId);
    Task<RestaurantTable> AddTableAsync(RestaurantTable restaurantTable);
    Task<RestaurantTable> UpdateTableAsync(RestaurantTable restaurantTable);
    Task<bool> DeleteTableAsync(int tableId);
}

