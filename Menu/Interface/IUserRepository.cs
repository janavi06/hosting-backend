using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllUsersAsync(int? restaurantId = null);
    Task<User> GetUserByIdAsync(int userId);
    Task<User> AddUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(int userId);
    Task<User?> GetAvailableWaiterAsync(int restaurantId);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task UpdateAvailabilityAsync(int userId, bool isAvailable);
    Task<IEnumerable<User>> GetUsersByRoleAsync(string role, int? restaurantId = null);

    Task<IEnumerable<User>> GetAllUsersByRestaurantAsync(int restaurantId);

}
