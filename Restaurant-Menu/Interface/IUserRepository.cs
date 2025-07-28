using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User> GetUserByIdAsync(int userId);
    Task<User> AddUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(int userId);
    Task<User> GetAvailableWaiterAsync();


    Task<User?> GetByEmailAsync(string email);


    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task UpdateAvailabilityAsync(int userId, bool isAvailable);

    // ✅ Add this method to filter users by role
    Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
}
