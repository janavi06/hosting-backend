using Restaurant_Menu.Models;

namespace Restaurant_Menu.Interface
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetCategoriesByRestaurantAsync(int restaurantId);
        Task<Category?> GetCategoryByIdAndRestaurantAsync(int categoryId, int restaurantId);
        Task<Category?> GetCategoryByIdAsync(int categoryId);
        Task AddCategoryAsync(Category category);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(int categoryId);
    }
}
