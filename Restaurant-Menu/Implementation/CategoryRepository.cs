using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Models;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;

    public CategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        return await _context.Categories
            .Include(c => c.SubCategories) // Include subcategories
                .ThenInclude(sc => sc.Products) // Include products under subcategories
            .Include(c => c.Products) // Include products directly under the category
            .Where(c => c.Products.Any() || c.SubCategories.Any()) // Ensure categories with or without subcategories are included
            .ToListAsync();
    }

    public async Task<Category> GetCategoryByIdAsync(int categoryId)
    {
        return await _context.Categories
            .Include(c => c.SubCategories) // Include subcategories
                .ThenInclude(sc => sc.Products) // Include products under subcategories
            .Include(c => c.Products) // Include products directly under the category
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
