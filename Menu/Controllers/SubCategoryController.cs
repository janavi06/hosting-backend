using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class SubCategoriesController : ControllerBase
{
    private readonly ISubCategoryRepository _subCategoryRepository;

    public SubCategoriesController(ISubCategoryRepository subCategoryRepository)
    {
        _subCategoryRepository = subCategoryRepository;
    }

    // ✅ GET: api/subcategories?restaurantId=5
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubCategory>>> GetSubCategories([FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("RestaurantID is required.");

        var subCategories = await _subCategoryRepository.GetSubCategoriesByRestaurantAsync(restaurantId);

        if (subCategories == null || !subCategories.Any())
        {
            return NotFound("No subcategories found for this restaurant.");
        }

        return Ok(subCategories);
    }

    // ✅ GET: api/subcategories/3?restaurantId=5
    [HttpGet("{id}")]
    public async Task<ActionResult<SubCategory>> GetSubCategory(int id, [FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("RestaurantID is required.");

        var subCategory = await _subCategoryRepository.GetSubCategoryByIdAsync(id);

        if (subCategory == null || subCategory.RestaurantID != restaurantId)
        {
            return NotFound($"SubCategory with ID {id} not found for this restaurant.");
        }

        return Ok(subCategory);
    }

    // ✅ POST: api/subcategories
    [HttpPost]
    public async Task<ActionResult<SubCategory>> CreateSubCategory([FromBody] SubCategory subCategory)
    {
        if (subCategory == null || subCategory.RestaurantID <= 0)
        {
            return BadRequest("SubCategory data and RestaurantID are required.");
        }

        await _subCategoryRepository.AddSubCategoryAsync(subCategory);
        return CreatedAtAction(nameof(GetSubCategory), new { id = subCategory.SubCategoryID, restaurantId = subCategory.RestaurantID }, subCategory);
    }

    // ✅ PUT: api/subcategories/3?restaurantId=5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSubCategory(int id, [FromBody] SubCategory subCategory, [FromQuery] int restaurantId)
    {
        if (id != subCategory.SubCategoryID)
        {
            return BadRequest("ID mismatch.");
        }

        if (subCategory.RestaurantID != restaurantId || restaurantId <= 0)
        {
            return BadRequest("Invalid or mismatched RestaurantID.");
        }

        var existing = await _subCategoryRepository.GetSubCategoryByIdAsync(id);
        if (existing == null || existing.RestaurantID != restaurantId)
        {
            return NotFound($"SubCategory with ID {id} not found for this restaurant.");
        }

        await _subCategoryRepository.UpdateSubCategoryAsync(subCategory);
        return NoContent();
    }

    // ✅ DELETE: api/subcategories/3?restaurantId=5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSubCategory(int id, [FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("RestaurantID is required.");

        var existing = await _subCategoryRepository.GetSubCategoryByIdAsync(id);
        if (existing == null || existing.RestaurantID != restaurantId)
        {
            return NotFound($"SubCategory with ID {id} not found for this restaurant.");
        }

        await _subCategoryRepository.DeleteSubCategoryAsync(id);
        return NoContent();
    }
}
