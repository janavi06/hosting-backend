using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class RestaurantTableRepository : IRestaurantTableRepository
{
    private readonly ApplicationDbContext _context;

    public RestaurantTableRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RestaurantTable>> GetAllTablesAsync(int? restaurantId = null)
    {
        var query = _context.RestaurantTables.AsQueryable();

        if (restaurantId.HasValue)
        {
            query = query.Where(t => t.RestaurantID == restaurantId.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<RestaurantTable> GetTableByIdAsync(int tableId)
    {
        return await _context.RestaurantTables
            .Include(t => t.Restaurant)
            .FirstOrDefaultAsync(t => t.RestaurantTableID == tableId);
    }

    public async Task<RestaurantTable?> GetTableByTableNameAsync(string tableIdentifier)
    {
        // Try to parse as number (table ID)
        if (int.TryParse(tableIdentifier, out int tableId))
        {
            return await _context.RestaurantTables
                .Include(rt => rt.Restaurant)
                .FirstOrDefaultAsync(rt => rt.RestaurantTableID == tableId);
        }

        // Otherwise search by table name
        return await _context.RestaurantTables
            .Include(rt => rt.Restaurant)
            .FirstOrDefaultAsync(rt => rt.TableName == tableIdentifier);
    }

    // ✅ NEW: Get table by table number and restaurant
    public async Task<RestaurantTable?> GetTableByTableNoAsync(int tableNo, int restaurantId)
    {
        return await _context.RestaurantTables
            .Include(t => t.Restaurant)
            .FirstOrDefaultAsync(t => t.TableNo == tableNo && t.RestaurantID == restaurantId);
    }

    // ✅ NEW: Get table by table number with restaurant info
    public async Task<RestaurantTable?> GetTableByTableNoWithRestaurantAsync(int tableNo)
    {
        return await _context.RestaurantTables
            .Include(t => t.Restaurant)
            .FirstOrDefaultAsync(t => t.TableNo == tableNo);
    }

    public async Task<RestaurantTable> AddTableAsync(RestaurantTable restaurantTable)
    {
        _context.RestaurantTables.Add(restaurantTable);
        await _context.SaveChangesAsync();
        return restaurantTable;
    }

    public async Task<IEnumerable<RestaurantTable>> GetAllTablesByRestaurantAsync(int restaurantId)
    {
        return await _context.RestaurantTables
            .Where(t => t.RestaurantID == restaurantId)
            .OrderBy(t => t.TableNo) // ✅ Order by table number
            .ToListAsync();
    }

    public async Task<RestaurantTable?> GetTableByIdWithRestaurantAsync(int tableId)
    {
        return await _context.RestaurantTables
            .Include(t => t.Restaurant)
            .FirstOrDefaultAsync(t => t.RestaurantTableID == tableId);
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