using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant_Menu.Interface
{
    public interface IOfferRepository
    {
        Task<Offer> AddOfferAsync(Offer offer);
        Task<List<Offer>> GetActiveOffersAsync(int restaurantId);
        Task<Offer?> GetOfferByIdAsync(int id);
        Task<bool> DeleteOfferAsync(int id);

        /// <summary>
        /// Return offers that are applicable for a restaurant given a bill amount and list of product IDs.
        /// Only returns offers that are currently active + autoApply == true.
        /// </summary>
        Task<List<Offer>> GetApplicableOffersAsync(int restaurantId, decimal billAmount, List<int> productIds);

        Task<object> GetOfferStatsAsync(int restaurantId);
        Task<object> GetOfferPerformanceAsync(int restaurantId);
    }
}
