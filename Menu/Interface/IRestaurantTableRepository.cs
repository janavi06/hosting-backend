using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IRestaurantTableRepository
{
    Task<IEnumerable<RestaurantTable>> GetAllTablesAsync(int? restaurantId = null);
    Task<IEnumerable<RestaurantTable>> GetAllTablesByRestaurantAsync(int restaurantId);
    Task<RestaurantTable> GetTableByIdAsync(int tableId);
    Task<RestaurantTable?> GetTableByTableNameAsync(string tableName);
    Task<RestaurantTable?> GetTableByIdWithRestaurantAsync(int tableId);

    Task<RestaurantTable> AddTableAsync(RestaurantTable restaurantTable);
    Task<RestaurantTable> UpdateTableAsync(RestaurantTable restaurantTable);
    Task<bool> DeleteTableAsync(int tableId);
}
