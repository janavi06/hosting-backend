using Microsoft.AspNetCore.Http;
using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllProductsAsync(int? categoryId = null, int? subCategoryId = null, int? restaurantId = null);
    Task<Product> GetProductByIdAsync(int productId);
    Task<Product> AddProductAsync(Product product, IFormFile? imageFile = null);
    Task<Product> UpdateProductAsync(Product product, IFormFile? imageFile = null);
    Task<bool> DeleteProductAsync(int productId);
    Task<string?> UploadImageAsync(IFormFile? imageFile);
    Task<IEnumerable<Category>> GetCategoriesWithProductsAsync(int? restaurantId = null);
    decimal GetProductPrice(int productId);
    Task<decimal> GetProductPriceAsync(int productId);
    Task<IEnumerable<Product>> GetProductsByVegFilterAsync(bool? isVeg, int? restaurantId = null);
    Task<bool> UpdateProductAvailabilityAsync(int productId, bool isAvailable);

    // ✅ Updated method to accept 3 parameters
    Task<IEnumerable<Product>> GetAllProductsByRestaurantAsync(int restaurantId, int? categoryId = null, int? subCategoryId = null);
}

