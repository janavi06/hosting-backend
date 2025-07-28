using Microsoft.AspNetCore.Mvc;
using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class RestaurantTableController : ControllerBase
{
    private readonly IRestaurantTableRepository _restaurantTableRepository;

    public RestaurantTableController(IRestaurantTableRepository restaurantTableRepository)
    {
        _restaurantTableRepository = restaurantTableRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RestaurantTable>>> GetAllTables()
    {
        var tables = await _restaurantTableRepository.GetAllTablesAsync();
        return Ok(tables);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RestaurantTable>> GetTableById(int id)
    {
        var table = await _restaurantTableRepository.GetTableByIdAsync(id);
        if (table == null)
        {
            return NotFound();
        }
        return Ok(table);
    }

    [HttpPost]
    public async Task<ActionResult<RestaurantTable>> AddTable(RestaurantTable restaurantTable)
    {
        var createdTable = await _restaurantTableRepository.AddTableAsync(restaurantTable);
        return CreatedAtAction(nameof(GetTableById), new { id = createdTable.RestaurantTableID }, createdTable);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTable(int id, RestaurantTable restaurantTable)
    {
        if (id != restaurantTable.RestaurantTableID)
        {
            return BadRequest();
        }

        await _restaurantTableRepository.UpdateTableAsync(restaurantTable);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTable(int id)
    {
        var deleted = await _restaurantTableRepository.DeleteTableAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}