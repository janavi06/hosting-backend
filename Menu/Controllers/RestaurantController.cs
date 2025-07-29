using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Models;
using System.Threading.Tasks;

namespace Restaurant_Menu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RestaurantController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public RestaurantController(ApplicationDbContext db) => _db = db;

        // GET: api/restaurant
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Restaurant>>> GetAll()
        {
            var restaurants = await _db.Restaurants.ToListAsync();
            return Ok(restaurants);
        }

        // GET: api/restaurant/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Restaurant>> GetById(int id)
        {
            var restaurant = await _db.Restaurants.FindAsync(id);
            if (restaurant == null) return NotFound();
            return Ok(restaurant);
        }

        // POST: api/restaurant
        [HttpPost]
        public async Task<ActionResult<Restaurant>> Create([FromBody] Restaurant input)
        {
            if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.UPI_ID))
                return BadRequest("Name and UPI_ID are required.");

            _db.Restaurants.Add(input);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = input.RestaurantID }, input);
        }

        // PUT: api/restaurant/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Restaurant input)
        {
            if (id != input.RestaurantID)
                return BadRequest("ID mismatch.");

            var existing = await _db.Restaurants.FindAsync(id);
            if (existing == null) return NotFound();

            // Update only editable fields
            existing.Name = input.Name;
            existing.Description = input.Description;
            existing.LogoPath = input.LogoPath;
            existing.UPI_ID = input.UPI_ID;
            existing.UPI_Name = input.UPI_Name;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/restaurant/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _db.Restaurants.FindAsync(id);
            if (r == null) return NotFound();

            _db.Restaurants.Remove(r);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
