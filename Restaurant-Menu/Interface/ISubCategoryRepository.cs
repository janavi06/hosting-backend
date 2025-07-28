using Restaurant_Menu.Models;

namespace Restaurant_Menu.Interface
{
    public interface ISubCategoryRepository
    {
        Task<IEnumerable<SubCategory>> GetSubCategoriesAsync();
        Task<SubCategory> GetSubCategoryByIdAsync(int subCategoryId);
        Task AddSubCategoryAsync(SubCategory subCategory);
        Task UpdateSubCategoryAsync(SubCategory subCategory);
        Task DeleteSubCategoryAsync(int subCategoryId);
    }
}
