using Microsoft.AspNetCore.Mvc;
using Restaurant_Menu.Models;
using Restaurant_Menu.Interface;

[ApiController]
[Route("api/[controller]")]
public class OfferController : ControllerBase
{
    private readonly IOfferRepository _repo;

    public OfferController(IOfferRepository repo)
    {
        _repo = repo;
    }

    // 🔹 POST: api/offer
    [HttpPost]
    public async Task<IActionResult> AddOffer([FromBody] Offer offer)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (offer.RestaurantID <= 0)
            return BadRequest("RestaurantID is required.");

        if (offer.ValidFrom >= offer.ValidTo)
            return BadRequest("ValidFrom must be before ValidTo");

        if (!offer.DiscountAmount.HasValue && !offer.DiscountPercent.HasValue)
            return BadRequest("Either discount amount or percent must be specified");

        try
        {
            var created = await _repo.AddOfferAsync(offer);
            return Ok(created);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
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
        var offer = await _repo.GetOfferByIdAsync(id);
        if (offer == null || offer.RestaurantID != restaurantId)
            return NotFound("Offer not found or doesn't belong to the specified restaurant");

        var result = await _repo.DeleteOfferAsync(id);
        return result ? Ok() : StatusCode(500, "Failed to delete the offer");
    }
}
