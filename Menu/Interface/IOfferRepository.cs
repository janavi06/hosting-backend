using Restaurant_Menu.Models;

namespace Restaurant_Menu.Interface
{
    public interface IOfferRepository
    {
        Task<Offer> AddOfferAsync(Offer offer);
        Task<List<Offer>> GetActiveOffersAsync(int restaurantId);
        Task<Offer?> GetOfferByIdAsync(int id);
        Task<bool> DeleteOfferAsync(int id);
    }
}
