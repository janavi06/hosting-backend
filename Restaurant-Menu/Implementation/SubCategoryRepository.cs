using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Models;

namespace Restaurant_Menu.Implementation
{
    public class SubCategoryRepository : ISubCategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public SubCategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SubCategory>> GetSubCategoriesAsync()
        {
            return await _context.SubCategories
                .Include(sc => sc.Category)  // Include Category
                .Include(sc => sc.Products) // Include Products
                .ToListAsync();
        }

        public async Task<SubCategory> GetSubCategoryByIdAsync(int subCategoryId)
        {
            return await _context.SubCategories
                .Include(sc => sc.Category)
                .Include(sc => sc.Products) // Include Products
                .FirstOrDefaultAsync(sc => sc.SubCategoryID == subCategoryId);
        }

        public async Task AddSubCategoryAsync(SubCategory subCategory)
        {
            _context.SubCategories.Add(subCategory);
            await _context.SaveChangesAsync();
        }


        public async Task<IEnumerable<Category>> GetCategoriesWithProductsAsync()
        {
            var categories = await _context.Categories
                .Include(c => c.SubCategories)
                .ThenInclude(sc => sc.Products) // Include products in subcategories
                .Include(c => c.Products) // Include products directly under categories
                .ToListAsync();

            foreach (var category in categories)
            {
                // Include products in subcategories
                foreach (var subCategory in category.SubCategories)
                {
                    subCategory.Products = subCategory.Products ?? new List<Product>();
                }

                // Find products that are directly under the category (not in subcategories)
                var categoryProducts = category.Products
                    .Where(p => p.SubCategoryID == null) // No subcategory associated
                    .ToList();

                category.Products = categoryProducts.Concat(category.Products.Where(p => p.SubCategoryID != null)).ToList();
            }

            return categories;
        }


        public async Task UpdateSubCategoryAsync(SubCategory subCategory)
        {
            _context.Entry(subCategory).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSubCategoryAsync(int subCategoryId)
        {
            var subCategory = await _context.SubCategories.FindAsync(subCategoryId);
            if (subCategory != null)
            {
                _context.SubCategories.Remove(subCategory);
                await _context.SaveChangesAsync();
            }
        }
    }
}
