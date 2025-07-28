using Microsoft.AspNetCore.Http;
using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IProductRepository
{
    // ✅ Get all products (full menu) with optional categoryId and subCategoryId filters
    Task<IEnumerable<Product>> GetAllProductsAsync(int? categoryId = null, int? subCategoryId = null);

    // ✅ Get a product by its ID
    Task<Product> GetProductByIdAsync(int productId);

    // ✅ Add a new product (image is optional)
    Task<Product> AddProductAsync(Product product, IFormFile? imageFile = null);

    // ✅ Update an existing product (image update is optional)
    Task<Product> UpdateProductAsync(Product product, IFormFile? imageFile = null);

    // ✅ Delete a product by its ID
    Task<bool> DeleteProductAsync(int productId);

    // ✅ Upload an image and return its URL (nullable for no image)
    Task<string?> UploadImageAsync(IFormFile? imageFile);

    // ✅ Get categories with products inside subcategories and products directly under categories
    Task<IEnumerable<Category>> GetCategoriesWithProductsAsync();

    decimal GetProductPrice(int productId);

    Task<decimal> GetProductPriceAsync(int productId);

    Task<IEnumerable<Product>> GetProductsByVegFilterAsync(bool? isVeg);


    Task<bool> UpdateProductAvailabilityAsync(int productId, bool isAvailable);


}