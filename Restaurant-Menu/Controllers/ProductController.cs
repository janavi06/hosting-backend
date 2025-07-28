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

        // ✅ GET: api/product - Get all products with SubCategory & Category details
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAllProducts(int? categoryId = null, int? subCategoryId = null)
        {
            var products = await _productRepository.GetAllProductsAsync(categoryId, subCategoryId);
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

                // ← THIS IS THE NEW BIT: serialize all CustomizationOptions
                CustomizationOptions = p.CustomizationOptions
                    .Select(opt => new
                    {
                        opt.CustomizationOptionID,
                        opt.Name,
                        opt.FixedPrice
                    })
                    .ToList()
            })
            .ToList();

            return Ok(result);
        }



        // ✅ GET: api/product/{id} - Get a single product by ID
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetProductById(int id)
        {
            var p = await _productRepository.GetProductByIdAsync(id);
            if (p == null)
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
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            if (product == null || string.IsNullOrWhiteSpace(product.ProductName) || product.Price <= 0)
            {
                return BadRequest("❌ Invalid product data.");
            }

            // Validate category & subcategory
            if (!product.CategoryID.HasValue)
                return BadRequest("CategoryID is required.");

            var category = await _categoryRepository
                                  .GetCategoryByIdAsync(product.CategoryID.Value);

            if (category == null)
                return BadRequest("❌ Invalid CategoryID.");

            if (product.SubCategoryID.HasValue)
            {
                var subCategory = await _subCategoryRepository.GetSubCategoryByIdAsync(product.SubCategoryID.Value);
                if (subCategory == null)
                {
                    return BadRequest("❌ Invalid SubCategoryID.");
                }
            }

            // Assign default values if null
            product.ProductDescription ??= "";
            product.ImagePath ??= "";
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;

            var createdProduct = await _productRepository.AddProductAsync(product);
            return CreatedAtAction(nameof(GetProductById), new { id = createdProduct.ProductID }, createdProduct);
        }

        // ✅ PUT: api/product/{id} - Update product (Only Manager)

        // ProductController.cs
        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> UpdateProduct(int id, Product product)
        {

            // Add manual validation for required fields
            if (string.IsNullOrWhiteSpace(product.ProductName))
                ModelState.AddModelError("ProductName", "Product name is required");

            if (product.Price <= 0)
                ModelState.AddModelError("Price", "Price must be greater than 0");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);



            if (id != product.ProductID) return BadRequest("ID mismatch");

            var existing = await _productRepository.GetProductByIdAsync(id);
            if (existing == null) return NotFound();

            // Validate category exists
            if (!product.CategoryID.HasValue)
                return BadRequest("CategoryID is required.");

            var category = await _categoryRepository
                                  .GetCategoryByIdAsync(product.CategoryID.Value);

            if (category == null)
                return BadRequest("❌ Invalid CategoryID.");


            // Validate subcategory if provided
            if (product.SubCategoryID.HasValue)
            {
                var subCategory = await _subCategoryRepository.GetSubCategoryByIdAsync(product.SubCategoryID.Value);
                if (subCategory == null || subCategory.CategoryID != product.CategoryID)
                    return BadRequest("Subcategory doesn't belong to selected category");
            }

            // Update only allowed fields
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

        [HttpGet("filter")]
        public async Task<IActionResult> GetProductsByVegFilter([FromQuery] bool? isVeg)
        {
            var products = await _productRepository.GetProductsByVegFilterAsync(isVeg);
            return Ok(products);
        }


        [HttpPut("{productId}/availability")]
        public async Task<IActionResult> UpdateAvailability(int productId, [FromBody] bool isAvailable)
        {
            var result = await _productRepository.UpdateProductAvailabilityAsync(productId, isAvailable);
            if (result)
                return Ok("Product availability updated successfully.");
            else
                return NotFound("Product not found.");
        }



        // ✅ DELETE: api/product/{id} - Delete product (Only Manager)
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            var existingProduct = await _productRepository.GetProductByIdAsync(id);
            if (existingProduct == null)
            {
                return NotFound(new { message = "❌ Product not found." });
            }

            await _productRepository.DeleteProductAsync(id);
            return NoContent();
        }

        // ✅ GET: api/categories-with-products - Get categories with products inside subcategories and products directly under categories
        [HttpGet("categories-with-products")]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategoriesWithProducts()
        {
            var categories = await _productRepository.GetCategoriesWithProductsAsync();
            if (categories == null || !categories.Any())
            {
                return NotFound("❌ No categories with products found.");
            }

            return Ok(categories);
        }
    }
}
