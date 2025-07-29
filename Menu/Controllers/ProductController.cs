using Microsoft.AspNetCore.Mvc;
using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Restaurant_Menu.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Restaurant_Menu.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly ISubCategoryRepository _subCategoryRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderItemRepository _orderItemRepository;

        public ProductController(IProductRepository productRepository,
                                 ISubCategoryRepository subCategoryRepository,
                                 ICategoryRepository categoryRepository,
                                 IOrderRepository orderRepository,
                                 IOrderItemRepository orderItemRepository)
        {
            _productRepository = productRepository;
            _subCategoryRepository = subCategoryRepository;
            _categoryRepository = categoryRepository;
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
        }

        // ✅ GET: api/product?restaurantId=5
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAllProducts(
            [FromQuery] int restaurantId,
            [FromQuery] int? categoryId = null,
            [FromQuery] int? subCategoryId = null)
        {
            if (restaurantId <= 0)
                return BadRequest("RestaurantID is required.");

            var products = await _productRepository.GetAllProductsByRestaurantAsync(restaurantId, categoryId, subCategoryId);
            if (products == null || !products.Any())
            {
                return NotFound("❌ No products found.");
            }

            var result = products.Select(p => new
            {
                p.ProductID,
                p.ProductName,
                p.Price,
                p.ProductDescription,
                p.ImagePath,
                p.CategoryID,
                p.SubCategoryID,
                p.IsAvailable,

                SubCategory = p.SubCategory == null
                    ? null
                    : new
                    {
                        p.SubCategory.SubCategoryID,
                        p.SubCategory.SubCategoryName,
                        p.SubCategory.CategoryID
                    },

                Category = p.Category == null
                    ? null
                    : new
                    {
                        p.Category.CategoryID,
                        p.Category.CategoryName
                    },

                CustomizationOptions = p.CustomizationOptions
                    .Select(opt => new
                    {
                        opt.CustomizationOptionID,
                        opt.Name,
                        opt.FixedPrice
                    })
                    .ToList()
            }).ToList();

            return Ok(result);
        }

        // ✅ GET: api/product/3?restaurantId=5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetProductById(int id, [FromQuery] int restaurantId)
        {
            if (restaurantId <= 0)
                return BadRequest("RestaurantID is required.");

            var p = await _productRepository.GetProductByIdAsync(id);
            if (p == null || p.RestaurantID != restaurantId)
                return NotFound(new { message = "❌ Product not found." });

            return Ok(new
            {
                p.ProductID,
                p.ProductName,
                p.Price,
                p.ProductDescription,
                p.ImagePath,
                p.CategoryID,
                p.SubCategoryID,
                p.IsAvailable,

                SubCategory = p.SubCategory == null
                    ? null
                    : new
                    {
                        p.SubCategory.SubCategoryID,
                        p.SubCategory.SubCategoryName,
                        p.SubCategory.CategoryID
                    },

                Category = p.Category == null
                    ? null
                    : new
                    {
                        p.Category.CategoryID,
                        p.Category.CategoryName
                    },

                CustomizationOptions = p.CustomizationOptions
                    .Select(opt => new
                    {
                        opt.CustomizationOptionID,
                        opt.Name,
                        opt.FixedPrice
                    })
                    .ToList()
            });
        }

        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct([FromBody] Product product)
        {
            if (product == null || string.IsNullOrWhiteSpace(product.ProductName) || product.Price <= 0)
                return BadRequest("❌ Invalid product data.");

            if (product.RestaurantID <= 0)
                return BadRequest("RestaurantID is required.");

            if (!product.CategoryID.HasValue)
                return BadRequest("CategoryID is required.");

            var category = await _categoryRepository.GetCategoryByIdAsync(product.CategoryID.Value);
            if (category == null || category.RestaurantID != product.RestaurantID)
                return BadRequest("❌ Invalid or mismatched CategoryID.");

            if (product.SubCategoryID.HasValue)
            {
                var subCategory = await _subCategoryRepository.GetSubCategoryByIdAsync(product.SubCategoryID.Value);
                if (subCategory == null || subCategory.RestaurantID != product.RestaurantID)
                    return BadRequest("❌ Invalid or mismatched SubCategoryID.");
            }

            product.ProductDescription ??= "";
            product.ImagePath ??= "";
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;

            var createdProduct = await _productRepository.AddProductAsync(product);
            return CreatedAtAction(nameof(GetProductById), new { id = createdProduct.ProductID, restaurantId = createdProduct.RestaurantID }, createdProduct);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> UpdateProduct(int id, [FromBody] Product product, [FromQuery] int restaurantId)
        {
            if (restaurantId <= 0)
                return BadRequest("RestaurantID is required.");

            if (id != product.ProductID)
                return BadRequest("ID mismatch");

            var existing = await _productRepository.GetProductByIdAsync(id);
            if (existing == null || existing.RestaurantID != restaurantId)
                return NotFound();

            if (!product.CategoryID.HasValue)
                return BadRequest("CategoryID is required.");

            var category = await _categoryRepository.GetCategoryByIdAsync(product.CategoryID.Value);
            if (category == null || category.RestaurantID != restaurantId)
                return BadRequest("❌ Invalid CategoryID.");

            if (product.SubCategoryID.HasValue)
            {
                var subCategory = await _subCategoryRepository.GetSubCategoryByIdAsync(product.SubCategoryID.Value);
                if (subCategory == null || subCategory.RestaurantID != restaurantId)
                    return BadRequest("Subcategory doesn't belong to this restaurant.");
            }

            existing.ProductName = product.ProductName;
            existing.Price = product.Price;
            existing.ProductDescription = product.ProductDescription;
            existing.CategoryID = product.CategoryID;
            existing.SubCategoryID = product.SubCategoryID;
            existing.IsAvailable = product.IsAvailable;
            existing.UpdatedAt = DateTime.UtcNow;

            await _productRepository.UpdateProductAsync(existing);
            return NoContent();
        }

        [HttpPut("{productId}/availability")]
        public async Task<IActionResult> UpdateAvailability(int productId, [FromQuery] int restaurantId, [FromBody] bool isAvailable)
        {
            var product = await _productRepository.GetProductByIdAsync(productId);
            if (product == null || product.RestaurantID != restaurantId)
                return NotFound("Product not found.");

            var result = await _productRepository.UpdateProductAvailabilityAsync(productId, isAvailable);
            return result
                ? Ok("Product availability updated successfully.")
                : StatusCode(500, "Failed to update availability.");
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProduct(int id, [FromQuery] int restaurantId)
        {
            var existingProduct = await _productRepository.GetProductByIdAsync(id);
            if (existingProduct == null || existingProduct.RestaurantID != restaurantId)
                return NotFound(new { message = "❌ Product not found." });

            await _productRepository.DeleteProductAsync(id);
            return NoContent();
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetProductsByVegFilter([FromQuery] int restaurantId, [FromQuery] bool? isVeg)
        {
            var products = await _productRepository.GetProductsByVegFilterAsync(isVeg, restaurantId);
            return Ok(products);
        }

        [HttpGet("categories-with-products")]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategoriesWithProducts([FromQuery] int restaurantId)
        {
            var categories = await _productRepository.GetCategoriesWithProductsAsync(restaurantId);
            if (categories == null || !categories.Any())
                return NotFound("❌ No categories with products found.");

            return Ok(categories);
        }
    }
}
