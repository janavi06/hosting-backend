using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Restaurant_Menu.Repositories
{
    public class OfferRepository : IOfferRepository
    {
        private readonly ApplicationDbContext _context;

        public OfferRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Offer> AddOfferAsync(Offer offer)
        {
            if (offer == null)
                throw new ArgumentNullException(nameof(offer));

            _context.Offers.Add(offer);
            await _context.SaveChangesAsync();
            return offer;
        }

        public async Task<List<Offer>> GetActiveOffersAsync(int restaurantId)
        {
            var now = DateTime.UtcNow;

            return await _context.Offers
                .Include(o => o.OfferProducts)
                .Where(o =>
                    o.RestaurantID == restaurantId &&
                    o.IsActive &&
                    o.ValidFrom <= now &&
                    o.ValidTo >= now)
                .ToListAsync();
        }

        public async Task<Offer?> GetOfferByIdAsync(int id)
        {
            return await _context.Offers
                .Include(o => o.OfferProducts)
                .FirstOrDefaultAsync(o => o.OfferID == id);
        }

        public async Task<bool> DeleteOfferAsync(int id)
        {
            var offer = await _context.Offers.FindAsync(id);
            if (offer == null)
                return false;

            _context.Offers.Remove(offer);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Return auto-apply offers applicable to bill + products.
        /// </summary>
        public async Task<List<Offer>> GetApplicableOffersAsync(
            int restaurantId,
            decimal billAmount,
            List<int> productIds)
        {
            var now = DateTime.UtcNow;

            var offers = await _context.Offers
                .Include(o => o.OfferProducts)
                .Where(o =>
                    o.RestaurantID == restaurantId &&
                    o.IsActive &&
                    o.AutoApply &&
                    o.ValidFrom <= now &&
                    o.ValidTo >= now)
                .ToListAsync();

            var applicable = offers.Where(o =>
            {
                if (o.Scope == "GLOBAL")
                    return true;

                if (o.Scope == "MIN_BILL")
                    return billAmount >= o.MinBillAmount;

                if (o.Scope == "PRODUCT_BASED")
                    return o.OfferProducts.Any(op =>
                        productIds.Contains(op.ProductID));

                return false;

            }).ToList();

            return applicable;
        }

        public async Task<object> GetOfferStatsAsync(int restaurantId)
        {
            var now = DateTime.UtcNow;

            var totalOffers = await _context.Offers
                .CountAsync(o => o.RestaurantID == restaurantId);

            var activeOffers = await _context.Offers
                .CountAsync(o =>
                    o.RestaurantID == restaurantId &&
                    o.IsActive &&
                    o.ValidFrom <= now &&
                    o.ValidTo >= now);

            return new
            {
                totalOffers,
                activeOffers,
                totalDiscounts = 0m,
                ordersWithOffers = 0
            };
        }

        public async Task<object> GetOfferPerformanceAsync(int restaurantId)
        {
            return new
            {
                Labels = new[] { "Week 1", "Week 2", "Week 3", "Week 4" },
                Orders = new[] { 0, 0, 0, 0 },
                Discounts = new[] { 0.0, 0.0, 0.0, 0.0 }
            };
        }
    }
}
