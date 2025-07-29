using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant_Menu.Interface
{
    public interface ISubCategoryRepository
    {
        Task<IEnumerable<SubCategory>> GetSubCategoriesAsync(int? restaurantId = null);
        Task<SubCategory> GetSubCategoryByIdAsync(int subCategoryId);
        Task AddSubCategoryAsync(SubCategory subCategory);
        Task UpdateSubCategoryAsync(SubCategory subCategory);
        Task DeleteSubCategoryAsync(int subCategoryId);
        Task<IEnumerable<SubCategory>> GetSubCategoriesByRestaurantAsync(int restaurantId);

        Task<IEnumerable<Category>> GetCategoriesWithProductsAsync(int restaurantId);
    }
}
