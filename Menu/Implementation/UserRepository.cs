using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Restaurant_Menu.Models;

namespace Restaurant_Menu.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _hasher = new();

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync(int? restaurantId = null)
        {
            var query = _context.Users.AsQueryable();
            if (restaurantId.HasValue)
            {
                query = query.Where(u => u.RestaurantID == restaurantId.Value);
            }
            return await query.ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int userId)
            => await _context.Users.FindAsync(userId);

        public async Task<User> AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<User?> GetAvailableWaiterAsync(int restaurantId)
            => await _context.Users
                .Where(u => u.UserRole == "Waiter" && u.RestaurantID == restaurantId)
                .Include(u => u.AssignedOrders)
                .OrderBy(u => u.AssignedOrders.Count)
                .FirstOrDefaultAsync();

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role, int? restaurantId = null)
        {
            var query = _context.Users
                .Where(u => u.UserRole == role)
                .AsQueryable();

            if (restaurantId.HasValue)
                query = query.Where(u => u.RestaurantID == restaurantId.Value);

            return await query.ToListAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
            => await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash!, currentPassword);
            if (result == PasswordVerificationResult.Failed)
                return false;

            user.PasswordHash = _hasher.HashPassword(user, newPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<IEnumerable<User>> GetAllUsersByRestaurantAsync(int restaurantId)
        {
            return await _context.Users
                .Where(u => u.RestaurantID == restaurantId)
                .ToListAsync();
        }

        public async Task UpdateAvailabilityAsync(int userId, bool isAvailable)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            user.IsAvailable = isAvailable;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
