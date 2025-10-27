using Microsoft.AspNetCore.Mvc;
using Restaurant_Menu.Models;
using Restaurant_Menu.Interface;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class OfferController : ControllerBase
{
    private readonly IOfferRepository _repo;
    private readonly ApplicationDbContext _context; // Add this

    public OfferController(IOfferRepository repo, ApplicationDbContext context) // Add context to constructor
    {
        _repo = repo;
        _context = context;
    }

    // In OfferController.cs - Update the AddOffer method
    [HttpPost]
    public async Task<IActionResult> AddOffer([FromQuery] int restaurantId, [FromBody] Offer offer)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // ✅ CRITICAL FIX: Set the RestaurantID from the query parameter
        offer.RestaurantID = restaurantId;

        if (offer.RestaurantID <= 0)
            return BadRequest("RestaurantID is required.");

        // Validate restaurant exists
        var restaurantExists = await _context.Restaurants.AnyAsync(r => r.RestaurantID == restaurantId);
        if (!restaurantExists)
            return BadRequest("Invalid restaurant ID");

        if (offer.ValidFrom >= offer.ValidTo)
            return BadRequest("ValidFrom must be before ValidTo");

        if (!offer.DiscountAmount.HasValue && !offer.DiscountPercent.HasValue)
            return BadRequest("Either discount amount or percent must be specified");

        try
        {
            var created = await _repo.AddOfferAsync(offer);
            return Ok(new
            {
                message = "Offer created successfully",
                offer = created
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
    // 🔹 POST: api/offer/bulk?restaurantId=5
    [HttpPost("bulk")]
    public async Task<IActionResult> AddBulkOffers([FromQuery] int restaurantId, [FromBody] List<Offer> offers)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Validate restaurant exists
        var restaurantExists = await _context.Restaurants.AnyAsync(r => r.RestaurantID == restaurantId);
        if (!restaurantExists)
            return BadRequest("Invalid RestaurantID");

        // Set RestaurantID for all offers
        foreach (var offer in offers)
        {
            offer.RestaurantID = restaurantId;
        }

        try
        {
            var createdOffers = new List<Offer>();
            foreach (var offer in offers)
            {
                if (offer.ValidFrom >= offer.ValidTo)
                    return BadRequest($"ValidFrom must be before ValidTo for offer: {offer.Description}");

                if (!offer.DiscountAmount.HasValue && !offer.DiscountPercent.HasValue)
                    return BadRequest($"Either discount amount or percent must be specified for offer: {offer.Description}");

                var created = await _repo.AddOfferAsync(offer);
                createdOffers.Add(created);
            }

            return Ok(new
            {
                message = $"Successfully created {createdOffers.Count} offers",
                offers = createdOffers
            });
        }
        catch (DbUpdateException dbEx)
        {
            return StatusCode(500, $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/offer/stats?restaurantId=5
    [HttpGet("stats")]
    public async Task<IActionResult> GetOfferStats([FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("Invalid RestaurantID");

        var stats = await _repo.GetOfferStatsAsync(restaurantId);
        return Ok(stats);
    }

    // GET: api/offer/performance?restaurantId=5
    [HttpGet("performance")]
    public async Task<IActionResult> GetOfferPerformance([FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("Invalid RestaurantID");

        var performance = await _repo.GetOfferPerformanceAsync(restaurantId);
        return Ok(performance);
    }

    // 🔹 GET: api/offer/restaurant/5
    [HttpGet("restaurant/{restaurantId}")]
    public async Task<IActionResult> GetOffers(int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("Invalid RestaurantID");

        var offers = await _repo.GetActiveOffersAsync(restaurantId);
        return Ok(offers);
    }

    // 🔹 GET: api/offer/10?restaurantId=5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, [FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("RestaurantID is required.");

        var offer = await _repo.GetOfferByIdAsync(id);
        if (offer == null || offer.RestaurantID != restaurantId)
            return NotFound("Offer not found for this restaurant");

        return Ok(offer);
    }

    // 🔹 DELETE: api/offer/10?restaurantId=5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("Invalid RestaurantID");

        var offer = await _repo.GetOfferByIdAsync(id);
        if (offer == null || offer.RestaurantID != restaurantId)
            return NotFound("Offer not found or doesn't belong to the specified restaurant");

        var result = await _repo.DeleteOfferAsync(id);
        return result ? Ok(new { message = "Offer deleted successfully" }) : StatusCode(500, "Failed to delete the offer");
    }

    // 🔹 PUT: api/offer/10?restaurantId=5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOffer(int id, [FromQuery] int restaurantId, [FromBody] Offer updatedOffer)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (restaurantId <= 0)
            return BadRequest("Invalid RestaurantID");

        var existingOffer = await _repo.GetOfferByIdAsync(id);
        if (existingOffer == null || existingOffer.RestaurantID != restaurantId)
            return NotFound("Offer not found for this restaurant");

        // Update properties
        existingOffer.Code = updatedOffer.Code;
        existingOffer.Description = updatedOffer.Description;
        existingOffer.DiscountAmount = updatedOffer.DiscountAmount;
        existingOffer.DiscountPercent = updatedOffer.DiscountPercent;
        existingOffer.MinBillAmount = updatedOffer.MinBillAmount;
        existingOffer.ValidFrom = updatedOffer.ValidFrom;
        existingOffer.ValidTo = updatedOffer.ValidTo;
        existingOffer.IsActive = updatedOffer.IsActive;
        existingOffer.AutoApply = updatedOffer.AutoApply;

        if (existingOffer.ValidFrom >= existingOffer.ValidTo)
            return BadRequest("ValidFrom must be before ValidTo");

        if (!existingOffer.DiscountAmount.HasValue && !existingOffer.DiscountPercent.HasValue)
            return BadRequest("Either discount amount or percent must be specified");

        try
        {
            _context.Offers.Update(existingOffer);
            await _context.SaveChangesAsync();

            // Invalidate cache
            var cacheKey = $"offers_{restaurantId}";
            // You'll need to inject IMemoryCache or modify your repository to handle cache invalidation

            return Ok(new { message = "Offer updated successfully", offer = existingOffer });
        }
        catch (DbUpdateException dbEx)
        {
            return StatusCode(500, $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}