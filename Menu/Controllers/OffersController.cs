using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Restaurant_Menu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OfferController : ControllerBase
    {
        private readonly IOfferRepository _repo;
        private readonly ApplicationDbContext _context;

        public OfferController(IOfferRepository repo, ApplicationDbContext context)
        {
            _repo = repo;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddOffer(
            [FromQuery] int restaurantId,
            [FromBody] Offer offer,
            [FromQuery] List<int>? productIds)
        {
            if (restaurantId <= 0)
                return BadRequest("restaurantId is required");

            var exists = await _context.Restaurants
                .AnyAsync(r => r.RestaurantID == restaurantId);

            if (!exists)
                return BadRequest("Invalid restaurantId");

            offer.RestaurantID = restaurantId;

            if (offer.ValidFrom >= offer.ValidTo)
                return BadRequest("ValidFrom must be before ValidTo");

            // ================= STRICT DISCOUNT VALIDATION =================

            if (offer.DiscountType == "PERCENT")
            {
                if (!offer.DiscountPercent.HasValue || offer.DiscountPercent <= 0)
                    return BadRequest("Valid DiscountPercent required");

                offer.DiscountAmount = null;
            }
            else if (offer.DiscountType == "AMOUNT")
            {
                if (!offer.DiscountAmount.HasValue || offer.DiscountAmount <= 0)
                    return BadRequest("Valid DiscountAmount required");

                offer.DiscountPercent = null;
            }
            else
            {
                return BadRequest("Invalid DiscountType");
            }

            // ================= SCOPE VALIDATION =================

            if (offer.Scope == "MIN_BILL" && offer.MinBillAmount <= 0)
                return BadRequest("MinBillAmount must be greater than 0");

            if (offer.Scope == "PRODUCT_BASED" &&
                (productIds == null || !productIds.Any()))
                return BadRequest("ProductIds required for PRODUCT_BASED offer");

            _context.Offers.Add(offer);
            await _context.SaveChangesAsync();

            // Insert OfferProducts
            if (productIds != null && productIds.Any())
            {
                foreach (var pid in productIds)
                {
                    _context.OfferProducts.Add(new OfferProduct
                    {
                        OfferID = offer.OfferID,
                        ProductID = pid
                    });
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                message = "Offer created successfully",
                offer
            });
        }

        [HttpGet("restaurant/{restaurantId}")]
        public async Task<IActionResult> GetOffers(int restaurantId)
        {
            if (restaurantId <= 0)
                return BadRequest("Invalid restaurantId");

            var offers = await _repo.GetActiveOffersAsync(restaurantId);
            return Ok(offers);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] int restaurantId)
        {
            if (restaurantId <= 0)
                return BadRequest("restaurantId is required");

            var offer = await _repo.GetOfferByIdAsync(id);
            if (offer == null || offer.RestaurantID != restaurantId)
                return NotFound("Offer not found");

            var deleted = await _repo.DeleteOfferAsync(id);
            if (!deleted)
                return StatusCode(500, "Failed to delete offer");

            return Ok(new { message = "Offer deleted successfully" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOffer(
            int id,
            [FromQuery] int restaurantId,
            [FromBody] Offer updatedOffer)
        {
            if (restaurantId <= 0)
                return BadRequest("restaurantId required");

            var existing = await _repo.GetOfferByIdAsync(id);

            if (existing == null || existing.RestaurantID != restaurantId)
                return NotFound("Offer not found");

            if (updatedOffer.ValidFrom >= updatedOffer.ValidTo)
                return BadRequest("ValidFrom must be before ValidTo");

            existing.Name = updatedOffer.Name;
            existing.Description = updatedOffer.Description;
            existing.Code = updatedOffer.Code;
            existing.Scope = updatedOffer.Scope;
            existing.DiscountType = updatedOffer.DiscountType;
            existing.DiscountAmount = updatedOffer.DiscountAmount;
            existing.DiscountPercent = updatedOffer.DiscountPercent;
            existing.MinBillAmount = updatedOffer.MinBillAmount;
            existing.ValidFrom = updatedOffer.ValidFrom;
            existing.ValidTo = updatedOffer.ValidTo;
            existing.IsActive = updatedOffer.IsActive;
            existing.AutoApply = updatedOffer.AutoApply;
            existing.Priority = updatedOffer.Priority;

            _context.Offers.Update(existing);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Offer updated", offer = existing });
        }
    }
}
