using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IRestaurantTableRepository
{
    Task<IEnumerable<RestaurantTable>> GetAllTablesAsync(int? restaurantId = null);
    Task<RestaurantTable> GetTableByIdAsync(int tableId);
    Task<RestaurantTable?> GetTableByTableNameAsync(string tableIdentifier);
    Task<RestaurantTable?> GetTableByTableNoAsync(int tableNo, int restaurantId);
    Task<RestaurantTable?> GetTableByTableNoWithRestaurantAsync(int tableNo);
    Task<RestaurantTable> AddTableAsync(RestaurantTable restaurantTable);
    Task<IEnumerable<RestaurantTable>> GetAllTablesByRestaurantAsync(int restaurantId);
    Task<RestaurantTable?> GetTableByIdWithRestaurantAsync(int tableId);
    Task<RestaurantTable> UpdateTableAsync(RestaurantTable restaurantTable);
    Task<bool> DeleteTableAsync(int tableId);
}
