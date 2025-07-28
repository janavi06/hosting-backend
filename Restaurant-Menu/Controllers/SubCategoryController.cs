using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Models;
using System.Collections.Generic;
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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubCategory>>> GetSubCategories()
    {
        var subCategories = await _subCategoryRepository.GetSubCategoriesAsync();

        if (subCategories == null || !subCategories.Any())
        {
            return NotFound("No subcategories found.");
        }

        return Ok(subCategories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SubCategory>> GetSubCategory(int id)
    {
        var subCategory = await _subCategoryRepository.GetSubCategoryByIdAsync(id);

        if (subCategory == null)
        {
            return NotFound($"SubCategory with ID {id} not found.");
        }

        return Ok(subCategory);
    }

    [HttpPost]
    public async Task<ActionResult<SubCategory>> CreateSubCategory(SubCategory subCategory)
    {
        if (subCategory == null)
        {
            return BadRequest("SubCategory data is required.");
        }

        await _subCategoryRepository.AddSubCategoryAsync(subCategory);
        return CreatedAtAction(nameof(GetSubCategory), new { id = subCategory.SubCategoryID }, subCategory);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSubCategory(int id, SubCategory subCategory)
    {
        if (id != subCategory.SubCategoryID)
        {
            return BadRequest("ID mismatch.");
        }

        var existingSubCategory = await _subCategoryRepository.GetSubCategoryByIdAsync(id);
        if (existingSubCategory == null)
        {
            return NotFound($"SubCategory with ID {id} not found.");
        }

        await _subCategoryRepository.UpdateSubCategoryAsync(subCategory);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSubCategory(int id)
    {
        var subCategory = await _subCategoryRepository.GetSubCategoryByIdAsync(id);
        if (subCategory == null)
        {
            return NotFound($"SubCategory with ID {id} not found.");
        }

        await _subCategoryRepository.DeleteSubCategoryAsync(id);
        return NoContent();
    }
}
