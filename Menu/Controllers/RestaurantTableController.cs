using Microsoft.AspNetCore.Mvc;
using Restaurant_Menu.Models;
using Restaurant_Menu.Interface;
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

    // ✅ GET: api/restauranttable?restaurantId=5
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RestaurantTable>>> GetAllTables([FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("RestaurantID is required.");

        var tables = await _restaurantTableRepository.GetAllTablesByRestaurantAsync(restaurantId);
        return Ok(tables);
    }

    // ✅ POST: api/restauranttable
    [HttpPost]
    public async Task<ActionResult<RestaurantTable>> AddTable([FromBody] RestaurantTable restaurantTable)
    {
        if (restaurantTable.RestaurantID <= 0)
            return BadRequest("RestaurantID is required.");

        // Validate table number uniqueness
        var existingTable = await _restaurantTableRepository.GetTableByTableNoAsync(restaurantTable.TableNo, restaurantTable.RestaurantID);
        if (existingTable != null)
            return BadRequest($"Table number {restaurantTable.TableNo} already exists in this restaurant.");

        var createdTable = await _restaurantTableRepository.AddTableAsync(restaurantTable);
        return CreatedAtAction(nameof(GetTableById),
            new { id = createdTable.RestaurantTableID, restaurantId = createdTable.RestaurantID },
            createdTable);
    }

    // ✅ GET: api/restauranttable/info?tableNo=3
    [HttpGet("info")]
    public async Task<IActionResult> GetRestaurantInfoByTableNo([FromQuery] string tableIdentifier)
    {
        if (string.IsNullOrEmpty(tableIdentifier))
            return BadRequest("Table identifier is required");

        // 🔄 Try parsing as TableID (int)
        if (int.TryParse(tableIdentifier, out int tableId))
        {
            var tableById = await _restaurantTableRepository.GetTableByIdWithRestaurantAsync(tableId);

            if (tableById == null)
                return NotFound("Table not found");

            return Ok(new
            {
                restaurantID = tableById.Restaurant?.RestaurantID,
                name = tableById.Restaurant?.Name,
                description = tableById.Restaurant?.Description,
                logoPath = tableById.Restaurant?.LogoPath,
                tableId = tableById.RestaurantTableID,
                tableNo = tableById.TableNo // ✅ Added table number
            });
        }

        // 🔄 Try parsing as TableNo (int)
        if (int.TryParse(tableIdentifier, out int tableNo))
        {
            var tableByNo = await _restaurantTableRepository.GetTableByTableNoWithRestaurantAsync(tableNo);

            if (tableByNo != null)
            {
                return Ok(new
                {
                    restaurantID = tableByNo.Restaurant?.RestaurantID,
                    name = tableByNo.Restaurant?.Name,
                    description = tableByNo.Restaurant?.Description,
                    logoPath = tableByNo.Restaurant?.LogoPath,
                    tableId = tableByNo.RestaurantTableID,
                    tableNo = tableByNo.TableNo // ✅ Added table number
                });
            }
        }

        // 🔄 Else fallback to table name
        var table = await _restaurantTableRepository.GetTableByTableNameAsync(tableIdentifier);

        if (table == null)
            return NotFound("Table not found");

        return Ok(new
        {
            restaurantID = table.Restaurant?.RestaurantID,
            name = table.Restaurant?.Name,
            description = table.Restaurant?.Description,
            logoPath = table.Restaurant?.LogoPath,
            tableId = table.RestaurantTableID,
            tableNo = table.TableNo // ✅ Added table number
        });
    }

    // ✅ NEW: Get table by table number
    // GET: api/restauranttable/bynumber?tableNo=5&restaurantId=1
    [HttpGet("bynumber")]
    public async Task<ActionResult<RestaurantTable>> GetTableByTableNo([FromQuery] int tableNo, [FromQuery] int restaurantId)
    {
        if (tableNo <= 0)
            return BadRequest("TableNo is required.");

        if (restaurantId <= 0)
            return BadRequest("RestaurantID is required.");

        var table = await _restaurantTableRepository.GetTableByTableNoAsync(tableNo, restaurantId);
        if (table == null)
        {
            return NotFound($"Table number {tableNo} not found in restaurant {restaurantId}");
        }
        return Ok(table);
    }

    // ✅ GET: api/restauranttable/4?restaurantId=5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RestaurantTable>> GetTableById(int id, [FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("RestaurantID is required.");

        var table = await _restaurantTableRepository.GetTableByIdAsync(id);
        if (table == null || table.RestaurantID != restaurantId)
        {
            return NotFound();
        }
        return Ok(table);
    }

    // ✅ PUT: api/restauranttable/3?restaurantId=5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTable(int id, [FromBody] RestaurantTable restaurantTable, [FromQuery] int restaurantId)
    {
        if (id != restaurantTable.RestaurantTableID)
            return BadRequest("ID mismatch.");

        if (restaurantTable.RestaurantID != restaurantId || restaurantId <= 0)
            return BadRequest("Invalid or mismatched RestaurantID.");

        var existingTable = await _restaurantTableRepository.GetTableByIdAsync(id);
        if (existingTable == null || existingTable.RestaurantID != restaurantId)
            return NotFound("Table not found for this restaurant.");

        // Check if table number is being changed and if it's unique
        if (existingTable.TableNo != restaurantTable.TableNo)
        {
            var tableWithSameNo = await _restaurantTableRepository.GetTableByTableNoAsync(restaurantTable.TableNo, restaurantId);
            if (tableWithSameNo != null && tableWithSameNo.RestaurantTableID != id)
                return BadRequest($"Table number {restaurantTable.TableNo} already exists in this restaurant.");
        }

        await _restaurantTableRepository.UpdateTableAsync(restaurantTable);
        return NoContent();
    }

    // ✅ DELETE: api/restauranttable/3?restaurantId=5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTable(int id, [FromQuery] int restaurantId)
    {
        var existingTable = await _restaurantTableRepository.GetTableByIdAsync(id);
        if (existingTable == null || existingTable.RestaurantID != restaurantId)
            return NotFound();

        var deleted = await _restaurantTableRepository.DeleteTableAsync(id);
        return deleted ? NoContent() : StatusCode(500, "Failed to delete table.");
    }
}