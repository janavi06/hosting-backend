using Microsoft.AspNetCore.Mvc;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Models;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoriesController(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    // GET: api/categories - Fetch all categories with subcategories and products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
    {
        var categories = await _categoryRepository.GetCategoriesAsync();

        foreach (var category in categories)
        {
            if (category.SubCategories != null && category.SubCategories.Count > 0)
            {
                // If subcategories exist, ensure products are included
                foreach (var subCategory in category.SubCategories)
                {
                    if (subCategory.Products == null)
                    {
                        subCategory.Products = new List<Product>(); // Initialize products if null
                    }
                }
            }
            else
            {
                // If no subcategories, display products directly under the category
                if (category.Products == null)
                {
                    category.Products = new List<Product>(); // Initialize products if null
                }
            }
        }

        return Ok(categories);
    }


    // GET: api/categories/{id} - Fetch a single category with its subcategories and products
    [HttpGet("{id}")]
    public async Task<ActionResult<Category>> GetCategoryById(int id)
    {
        var category = await _categoryRepository.GetCategoryByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        // Ensure subcategory products are handled
        if (category.SubCategories != null && category.SubCategories.Count > 0)
        {
            foreach (var subCategory in category.SubCategories)
            {
                if (subCategory.Products == null)
                {
                    subCategory.Products = new List<Product>(); // Initialize products if null
                }
            }
        }
        else
        {
            if (category.Products == null)
            {
                category.Products = new List<Product>(); // Initialize products if null
            }
        }

        return Ok(category);
    }

    // POST: api/categories - Create a new category (Main or Subcategory)
    [HttpPost]
    public async Task<ActionResult<Category>> CreateCategory(Category category)
    {
        await _categoryRepository.AddCategoryAsync(category);
        return CreatedAtAction(nameof(GetCategoryById), new { id = category.CategoryID }, category);
    }

    // PUT: api/categories/{id} - Update a category
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, Category category)
    {
        if (id != category.CategoryID)
        {
            return BadRequest("Category ID mismatch.");
        }

        await _categoryRepository.UpdateCategoryAsync(category);
        return NoContent();
    }

    // DELETE: api/categories/{id} - Delete a category
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _categoryRepository.GetCategoryByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        await _categoryRepository.DeleteCategoryAsync(id);
        return NoContent();
    }



}

