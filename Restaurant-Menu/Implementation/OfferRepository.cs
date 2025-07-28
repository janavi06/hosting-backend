using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Models;

public class OfferRepository : IOfferRepository
{
    private readonly ApplicationDbContext _context;
    private static readonly MemoryCache _offerCache = new MemoryCache(new MemoryCacheOptions());
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public OfferRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Offer> AddOfferAsync(Offer offer)
    {
        _context.Offers.Add(offer);
        await _context.SaveChangesAsync();
        return offer;
    }

    public async Task<List<Offer>> GetActiveOffersAsync(int restaurantId)
    {
        var cacheKey = $"offers_{restaurantId}";

        if (_offerCache.TryGetValue(cacheKey, out List<Offer> cachedOffers))
        {
            return cachedOffers;
        }

        var now = DateTime.UtcNow;


        var offers = await _context.Offers
            .AsNoTracking()
            .Where(o => o.RestaurantID == restaurantId
                     && o.IsActive
                     && o.ValidFrom <= now
                     && o.ValidTo >= now)
            .ToListAsync();

        _offerCache.Set(cacheKey, offers, _cacheDuration);
        return offers;
    }

    public async Task<Offer?> GetOfferByIdAsync(int id)
    {
        return await _context.Offers.FindAsync(id);
    }

    public async Task<bool> DeleteOfferAsync(int id)
    {
        var offer = await _context.Offers.FindAsync(id);
        if (offer == null) return false;

        _context.Offers.Remove(offer);
        await _context.SaveChangesAsync();
        return true;
    }
}