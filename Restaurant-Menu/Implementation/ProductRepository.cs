using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Restaurant_Menu.Models;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ProductRepository(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // This method is the one expected by the interface
    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        return await _context.Products.ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsByVegFilterAsync(bool? isVeg)
    {
        var query = _context.Products.AsQueryable();

        if (isVeg.HasValue)
        {
            query = query.Where(p => p.IsVeg == isVeg.Value);
        }

        return await query.ToListAsync();
    }


    public decimal GetProductPrice(int productId)
    {
        var product = _context.Products.FirstOrDefault(p => p.ProductID == productId);
        return product?.Price ?? 0m;
    }


    // Get all products with optional Category & SubCategory filtering
    public async Task<IEnumerable<Product>> GetAllProductsAsync(int? categoryId = null, int? subCategoryId = null)
    {
        // NOTE: Declare as IQueryable<Product> so that .Where(...) can assign back to it
        IQueryable<Product> query = _context.Products
            .Include(p => p.SubCategory)
                .ThenInclude(sc => sc.Category)
            .Include(p => p.CustomizationOptions);   // eager‐load CustomizationOptions

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.SubCategory != null
                                   && p.SubCategory.CategoryID == categoryId.Value);
        }

        if (subCategoryId.HasValue)
        {
            query = query.Where(p => p.SubCategoryID == subCategoryId.Value);
        }

        return await query.ToListAsync();
    }


    public async Task<decimal> GetProductPriceAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        return product?.Price ?? 0;
    }

    // Get a single product by ID with SubCategory & Category
    public async Task<Product> GetProductByIdAsync(int productId)
    {
        var product = await _context.Products
            .Include(p => p.SubCategory)
            .ThenInclude(sc => sc.Category)
              .Include(p => p.CustomizationOptions)
            .FirstOrDefaultAsync(p => p.ProductID == productId);

        if (product == null)
            throw new KeyNotFoundException("Product not found.");

        product.ImagePath ??= "/uploads/default-image.jpg";
        product.ProductDescription ??= "No description available";

        return product;
    }

    // Add a new product with image upload & SubCategory validation
    public async Task<Product> AddProductAsync(Product product, IFormFile imageFile)
    {
        var subCategory = await _context.SubCategories.FindAsync(product.SubCategoryID);
        if (subCategory == null)
            throw new KeyNotFoundException("SubCategory not found.");

        product.ImagePath = imageFile != null ? await UploadImageAsync(imageFile) : null;
        product.ProductDescription = string.IsNullOrWhiteSpace(product.ProductDescription) ? null : product.ProductDescription;

        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<bool> UpdateProductAvailabilityAsync(int productId, bool isAvailable)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return false;

        product.IsAvailable = isAvailable;
        product.UpdatedAt = DateTime.UtcNow;

        _context.Products.Update(product);
        await _context.SaveChangesAsync();

        return true;
    }






    // Update an existing product with optional image & SubCategory validation
    public async Task<Product> UpdateProductAsync(Product updated, IFormFile? imageFile = null)
    {
        // 1) Fetch the existing product
        var existing = await _context.Products.FindAsync(updated.ProductID);
        if (existing == null)
            throw new KeyNotFoundException($"Product {updated.ProductID} not found.");

        // 2) Handle the imageFile (unchanged)
        if (imageFile != null)
        {
            existing.ImagePath = await UploadImageAsync(imageFile);
        }
        else if (updated.ImagePath == null)
        {
            existing.ImagePath = null;
        }

        // 3) Copy over scalar props
        existing.ProductName = updated.ProductName;
        existing.Price = updated.Price;
        existing.ProductDescription =
            string.IsNullOrWhiteSpace(updated.ProductDescription)
              ? null
              : updated.ProductDescription;
        existing.UpdatedAt = DateTime.UtcNow;

        // 4) Category (assuming you’ve already validated CategoryID upstream)
        existing.CategoryID = updated.CategoryID;

        // 5) SubCategory: only if one was provided
        if (updated.SubCategoryID.HasValue)
        {
            var sub = await _context.SubCategories
                                    .FindAsync(updated.SubCategoryID.Value);
            if (sub == null || sub.CategoryID != updated.CategoryID)
                // will become a 400 Bad Request in your controller
                throw new ArgumentException(
                    $"Invalid SubCategoryID {updated.SubCategoryID}");
            existing.SubCategoryID = updated.SubCategoryID;
        }
        else
        {
            // caller sent null → clear it
            existing.SubCategoryID = null;
        }

        // 6) Persist
        await _context.SaveChangesAsync();
        return existing;
    }

    // Delete a product
    public async Task<bool> DeleteProductAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

    // Upload image method
    public async Task<string> UploadImageAsync(IFormFile imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
            return null;

        string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await imageFile.CopyToAsync(fileStream);
        }

        return "/uploads/" + uniqueFileName;
    }

    // Get categories with products inside subcategories and products directly under categories
    public async Task<IEnumerable<Category>> GetCategoriesWithProductsAsync()
    {
        var categories = await _context.Categories
            .Include(c => c.SubCategories)           // Include subcategories
            .ThenInclude(sc => sc.Products)           // Include products in subcategories
            .Include(c => c.Products)                // Include products directly under categories
            .ToListAsync();

        foreach (var category in categories)
        {
            // Include products in subcategories
            foreach (var subCategory in category.SubCategories)
            {
                subCategory.Products = subCategory.Products ?? new List<Product>();
            }

            // Get products that belong to the category but not any subcategory
            var productsWithoutSubCategory = category.Products
                .Where(p => p.SubCategoryID == null)  // No subcategory ID means it belongs directly to the category
                .ToList();

            // Get products that belong to a subcategory
            var productsInSubCategories = category.Products
                .Where(p => p.SubCategoryID != null)  // These belong to subcategories
                .ToList();

            // Combine products from subcategories and those without subcategories
            category.Products = productsWithoutSubCategory.Concat(productsInSubCategories).ToList();
        }

        return categories;
    }
}