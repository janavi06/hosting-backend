using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;

    public CategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // 🔹 Filtered by Restaurant
    public async Task<IEnumerable<Category>> GetCategoriesByRestaurantAsync(int restaurantId)
    {
        return await _context.Categories
            .Where(c => c.RestaurantID == restaurantId)
            .Include(c => c.SubCategories)
                .ThenInclude(sc => sc.Products)
            .Include(c => c.Products)
            .ToListAsync();
    }

    // 🔹 Specific Category by ID and Restaurant
    public async Task<Category?> GetCategoryByIdAndRestaurantAsync(int categoryId, int restaurantId)
    {
        return await _context.Categories
            .Include(c => c.SubCategories)
                .ThenInclude(sc => sc.Products)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CategoryID == categoryId && c.RestaurantID == restaurantId);
    }

    // 🔹 Without filtering
    public async Task<Category?> GetCategoryByIdAsync(int categoryId)
    {
        return await _context.Categories
            .Include(c => c.SubCategories)
                .ThenInclude(sc => sc.Products)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CategoryID == categoryId);
    }

    public async Task AddCategoryAsync(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateCategoryAsync(Category category)
    {
        _context.Entry(category).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(int categoryId)
    {
        var category = await _context.Categories.FindAsync(categoryId);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }
}
