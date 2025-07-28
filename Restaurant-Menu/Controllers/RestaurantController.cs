using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Models;
using System;
using System.Threading.Tasks;


namespace YourApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RestaurantController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public RestaurantController(ApplicationDbContext db) => _db = db;

        // GET: api/restaurant
        [HttpGet]
        public async Task<ActionResult<Restaurant>> Get()
        {
            var r = await _db.Restaurants.FirstOrDefaultAsync();
            if (r == null) return NotFound();
            return Ok(r);
        }

        // POST: api/restaurant
        [HttpPost]
        public async Task<ActionResult<Restaurant>> Create(Restaurant input)
        {
            _db.Restaurants.Add(input);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = input.RestaurantID }, input);
        }

        // GET: api/restaurant/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Restaurant>> GetById(int id)
        {
            var r = await _db.Restaurants.FindAsync(id);
            if (r == null) return NotFound();
            return Ok(r);
        }

        // PUT: api/restaurant/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Restaurant input)
        {
            if (id != input.RestaurantID) return BadRequest();
            _db.Entry(input).State = EntityState.Modified;
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _db.Restaurants.AnyAsync(e => e.RestaurantID == id))
                    return NotFound();
                throw;
            }
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
