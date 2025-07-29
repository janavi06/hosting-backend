using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Restaurant_Menu.Implementation
{
    public class SubCategoryRepository : ISubCategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public SubCategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SubCategory>> GetSubCategoriesAsync(int? restaurantId = null)
        {
            var query = _context.SubCategories
                .Include(sc => sc.Category)
                .Include(sc => sc.Products)
                .AsQueryable();

            if (restaurantId.HasValue)
            {
                query = query.Where(sc => sc.RestaurantID == restaurantId.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<SubCategory> GetSubCategoryByIdAsync(int subCategoryId)
        {
            return await _context.SubCategories
                .Include(sc => sc.Category)
                .Include(sc => sc.Products)
                .FirstOrDefaultAsync(sc => sc.SubCategoryID == subCategoryId);
        }

        public async Task AddSubCategoryAsync(SubCategory subCategory)
        {
            _context.SubCategories.Add(subCategory);
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<SubCategory>> GetSubCategoriesByRestaurantAsync(int restaurantId)
        {
            return await _context.SubCategories
                .Where(sc => sc.RestaurantID == restaurantId)
                .ToListAsync();
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

        public async Task<IEnumerable<Category>> GetCategoriesWithProductsAsync(int restaurantId)
        {
            var categories = await _context.Categories
                .Where(c => c.RestaurantID == restaurantId)
                .Include(c => c.SubCategories.Where(sc => sc.RestaurantID == restaurantId))
                    .ThenInclude(sc => sc.Products.Where(p => p.RestaurantID == restaurantId))
                .Include(c => c.Products.Where(p => p.RestaurantID == restaurantId))
                .ToListAsync();

            foreach (var category in categories)
            {
                foreach (var subCategory in category.SubCategories)
                {
                    subCategory.Products ??= new List<Product>();
                }

                var categoryProducts = category.Products
                    .Where(p => p.SubCategoryID == null)
                    .ToList();

                var subCategoryProducts = category.Products
                    .Where(p => p.SubCategoryID != null)
                    .ToList();

                category.Products = categoryProducts.Concat(subCategoryProducts).ToList();
            }

            return categories;
        }
    }
}
