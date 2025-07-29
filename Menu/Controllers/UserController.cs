using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IUserRepository _userRepository;

    public UserController(ApplicationDbContext context, IUserRepository userRepository)
    {
        _context = context;
        _userRepository = userRepository;
    }

    // ✅ GET: api/user?restaurantId=5
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAllUsers([FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("RestaurantID is required.");

        var users = await _userRepository.GetAllUsersByRestaurantAsync(restaurantId);
        return Ok(users);
    }

    // ✅ GET: api/user/3?restaurantId=5
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUserById(int id, [FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("RestaurantID is required.");

        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null || user.RestaurantID != restaurantId)
        {
            return NotFound();
        }
        return Ok(user);
    }

    // ✅ POST: api/user
    [HttpPost]
    public async Task<ActionResult<User>> AddUser([FromBody] User user)
    {
        if (user.RestaurantID <= 0)
            return BadRequest("RestaurantID is required.");

        var createdUser = await _userRepository.AddUserAsync(user);
        return CreatedAtAction(nameof(GetUserById), new { id = createdUser.UserID, restaurantId = createdUser.RestaurantID }, createdUser);
    }

    // ✅ PUT: api/user/3?restaurantId=5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] User user, [FromQuery] int restaurantId)
    {
        if (id != user.UserID)
            return BadRequest();

        if (user.RestaurantID != restaurantId || restaurantId <= 0)
            return BadRequest("Invalid RestaurantID.");

        var existingUser = await _userRepository.GetUserByIdAsync(id);
        if (existingUser == null || existingUser.RestaurantID != restaurantId)
            return NotFound();

        await _userRepository.UpdateUserAsync(user);
        return NoContent();
    }

    // ✅ DELETE: api/user/3?restaurantId=5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id, [FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest("RestaurantID is required.");

        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null || user.RestaurantID != restaurantId)
            return NotFound();

        var deleted = await _userRepository.DeleteUserAsync(id);
        return deleted ? NoContent() : StatusCode(500, "Failed to delete user.");
    }

    // ✅ GET: api/user/me
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var user = await _userRepository.GetUserByIdAsync(userId);

        if (user == null) return NotFound();

        return Ok(new
        {
            user.UserID,
            user.UserName,
            user.Email,
            user.PhoneNumber,
            user.IsAvailable,
            user.RestaurantID
        });
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateUser([FromBody] User updatedUser)
    {
        var userId = GetCurrentUserId();
        var existingUser = await _userRepository.GetUserByIdAsync(userId);

        var validationErrors = new List<string>();

        if (string.IsNullOrWhiteSpace(updatedUser.UserName))
            validationErrors.Add("Username is required");

        if (string.IsNullOrWhiteSpace(updatedUser.Email) || !IsValidEmail(updatedUser.Email))
            validationErrors.Add("Valid email is required");

        if (validationErrors.Any())
            return BadRequest(new { Errors = validationErrors });

        existingUser.UserName = updatedUser.UserName;
        existingUser.Email = updatedUser.Email;
        existingUser.PhoneNumber = updatedUser.PhoneNumber;
        existingUser.UpdatedAt = DateTime.UtcNow;
        existingUser.UpdatedBy = userId.ToString();

        await _userRepository.UpdateUserAsync(existingUser);
        return NoContent();
    }

    [HttpPost("me/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] Dictionary<string, string> passwords)
    {
        if (!passwords.ContainsKey("currentPassword") || !passwords.ContainsKey("newPassword"))
            return BadRequest("Both current and new password are required");

        var userId = GetCurrentUserId();
        var result = await _userRepository.ChangePasswordAsync(
            userId,
            passwords["currentPassword"],
            passwords["newPassword"]
        );

        return result ? NoContent() : BadRequest("Current password is incorrect");
    }

    [HttpPatch("me/availability")]
    [Authorize(Roles = "Waiter")]
    public async Task<IActionResult> UpdateAvailability([FromBody] bool isAvailable)
    {
        var userId = GetCurrentUserId();
        await _userRepository.UpdateAvailabilityAsync(userId, isAvailable);
        return NoContent();
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
