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

    [HttpPost]
    public async Task<IActionResult> AddOffer([FromBody] Offer offer)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Additional validation
        if (offer.ValidFrom >= offer.ValidTo)
        {
            return BadRequest("ValidFrom must be before ValidTo");
        }

        if (!offer.DiscountAmount.HasValue && !offer.DiscountPercent.HasValue)
        {
            return BadRequest("Either discount amount or percent must be specified");
        }

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

    [HttpGet("restaurant/{restaurantId}")]
    public async Task<IActionResult> GetOffers(int restaurantId)
    {
        var offers = await _repo.GetActiveOffersAsync(restaurantId);
        return Ok(offers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var offer = await _repo.GetOfferByIdAsync(id);
        return offer == null ? NotFound() : Ok(offer);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _repo.DeleteOfferAsync(id);
        return result ? Ok() : NotFound();
    }
}