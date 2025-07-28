using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Models;

public class RestaurantTableRepository : IRestaurantTableRepository
{
    private readonly ApplicationDbContext _context;

    public RestaurantTableRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RestaurantTable>> GetAllTablesAsync()
    {
        return await _context.RestaurantTables.ToListAsync();
    }

    public async Task<RestaurantTable> GetTableByIdAsync(int tableId)
    {
        return await _context.RestaurantTables.FindAsync(tableId);
    }

    public async Task<RestaurantTable> AddTableAsync(RestaurantTable restaurantTable)
    {
        _context.RestaurantTables.Add(restaurantTable);
        await _context.SaveChangesAsync();
        return restaurantTable;
    }

    public async Task<RestaurantTable> UpdateTableAsync(RestaurantTable restaurantTable)
    {
        _context.RestaurantTables.Update(restaurantTable);
        await _context.SaveChangesAsync();
        return restaurantTable;
    }

    public async Task<bool> DeleteTableAsync(int tableId)
    {
        var table = await _context.RestaurantTables.FindAsync(tableId);
        if (table == null) return false;

        _context.RestaurantTables.Remove(table);
        await _context.SaveChangesAsync();
        return true;
    }
}
