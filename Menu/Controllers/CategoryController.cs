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

    // 🔹 GET: api/categories?restaurantId=5
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories([FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("RestaurantID is required.");

        var categories = await _categoryRepository.GetCategoriesByRestaurantAsync(restaurantId);

        foreach (var category in categories)
        {
            if (category.SubCategories != null && category.SubCategories.Count > 0)
            {
                foreach (var subCategory in category.SubCategories)
                {
                    subCategory.Products ??= new List<Product>();
                }
            }
            else
            {
                category.Products ??= new List<Product>();
            }
        }

        return Ok(categories);
    }

    // 🔹 GET: api/categories/{id}?restaurantId=5
    [HttpGet("{id}")]
    public async Task<ActionResult<Category>> GetCategoryById(int id, [FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("RestaurantID is required.");

        var category = await _categoryRepository.GetCategoryByIdAndRestaurantAsync(id, restaurantId);
        if (category == null)
        {
            return NotFound();
        }

        if (category.SubCategories != null && category.SubCategories.Count > 0)
        {
            foreach (var subCategory in category.SubCategories)
            {
                subCategory.Products ??= new List<Product>();
            }
        }
        else
        {
            category.Products ??= new List<Product>();
        }

        return Ok(category);
    }

    // 🔹 POST: api/categories
    [HttpPost]
    public async Task<ActionResult<Category>> CreateCategory([FromBody] Category category)
    {
        if (category.RestaurantID <= 0)
            return BadRequest("RestaurantID is required.");

        await _categoryRepository.AddCategoryAsync(category);
        return CreatedAtAction(nameof(GetCategoryById), new { id = category.CategoryID, restaurantId = category.RestaurantID }, category);
    }

    // 🔹 PUT: api/categories/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
    {
        if (id != category.CategoryID)
        {
            return BadRequest("Category ID mismatch.");
        }

        if (category.RestaurantID <= 0)
            return BadRequest("RestaurantID is required.");

        await _categoryRepository.UpdateCategoryAsync(category);
        return NoContent();
    }

    // 🔹 DELETE: api/categories/{id}?restaurantId=5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id, [FromQuery] int restaurantId)
    {
        var category = await _categoryRepository.GetCategoryByIdAndRestaurantAsync(id, restaurantId);
        if (category == null)
        {
            return NotFound();
        }

        await _categoryRepository.DeleteCategoryAsync(id);
        return NoContent();
    }
}
