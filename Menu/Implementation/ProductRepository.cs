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
    public async Task<IEnumerable<Product>> GetAllProductsByRestaurantAsync(int restaurantId, int? categoryId = null, int? subCategoryId = null)
    {
        IQueryable<Product> query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.SubCategory)
            .Include(p => p.CustomizationOptions)
            .Where(p => p.RestaurantID == restaurantId);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryID == categoryId.Value);

        if (subCategoryId.HasValue)
            query = query.Where(p => p.SubCategoryID == subCategoryId.Value);

        return await query.ToListAsync();
    }


    public async Task<IEnumerable<Product>> GetAllProductsAsync(int? categoryId = null, int? subCategoryId = null, int? restaurantId = null)
    {
        IQueryable<Product> query = _context.Products
            .Include(p => p.SubCategory).ThenInclude(sc => sc.Category)
            .Include(p => p.CustomizationOptions);

        if (restaurantId.HasValue)
        {
            query = query.Where(p => p.RestaurantID == restaurantId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.SubCategory != null && p.SubCategory.CategoryID == categoryId.Value);
        }

        if (subCategoryId.HasValue)
        {
            query = query.Where(p => p.SubCategoryID == subCategoryId.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<Product> GetProductByIdAsync(int productId)
    {
        var product = await _context.Products
            .Include(p => p.SubCategory).ThenInclude(sc => sc.Category)
            .Include(p => p.CustomizationOptions)
            .FirstOrDefaultAsync(p => p.ProductID == productId);

        if (product == null)
            throw new KeyNotFoundException("Product not found.");

        product.ImagePath ??= "/uploads/default-image.jpg";
        product.ProductDescription ??= "No description available";

        return product;
    }

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

    public async Task<Product> UpdateProductAsync(Product updated, IFormFile? imageFile = null)
    {
        var existing = await _context.Products.FindAsync(updated.ProductID);
        if (existing == null)
            throw new KeyNotFoundException($"Product {updated.ProductID} not found.");

        if (imageFile != null)
        {
            existing.ImagePath = await UploadImageAsync(imageFile);
        }
        else if (updated.ImagePath == null)
        {
            existing.ImagePath = null;
        }

        existing.ProductName = updated.ProductName;
        existing.Price = updated.Price;
        existing.ProductDescription = string.IsNullOrWhiteSpace(updated.ProductDescription) ? null : updated.ProductDescription;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.CategoryID = updated.CategoryID;
        existing.RestaurantID = updated.RestaurantID;

        if (updated.SubCategoryID.HasValue)
        {
            var sub = await _context.SubCategories.FindAsync(updated.SubCategoryID.Value);
            if (sub == null || sub.CategoryID != updated.CategoryID)
                throw new ArgumentException($"Invalid SubCategoryID {updated.SubCategoryID}");
            existing.SubCategoryID = updated.SubCategoryID;
        }
        else
        {
            existing.SubCategoryID = null;
        }

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteProductAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

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

    public async Task<IEnumerable<Category>> GetCategoriesWithProductsAsync(int? restaurantId = null)
    {
        var categories = await _context.Categories
            .Include(c => c.SubCategories).ThenInclude(sc => sc.Products)
            .Include(c => c.Products)
            .ToListAsync();

        if (restaurantId.HasValue)
        {
            foreach (var c in categories)
            {
                c.Products = c.Products?.Where(p => p.RestaurantID == restaurantId.Value).ToList();
                foreach (var sc in c.SubCategories)
                {
                    sc.Products = sc.Products?.Where(p => p.RestaurantID == restaurantId.Value).ToList();
                }
            }
        }

        return categories;
    }

    public decimal GetProductPrice(int productId)
    {
        var product = _context.Products.FirstOrDefault(p => p.ProductID == productId);
        return product?.Price ?? 0m;
    }

    public async Task<decimal> GetProductPriceAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        return product?.Price ?? 0;
    }

    public async Task<IEnumerable<Product>> GetProductsByVegFilterAsync(bool? isVeg, int? restaurantId = null)
    {
        var query = _context.Products.AsQueryable();

        if (isVeg.HasValue)
        {
            query = query.Where(p => p.IsVeg == isVeg.Value);
        }

        if (restaurantId.HasValue)
        {
            query = query.Where(p => p.RestaurantID == restaurantId.Value);
        }

        return await query.ToListAsync();
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
}
