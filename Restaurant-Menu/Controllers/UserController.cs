using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Restaurant_Menu.Models;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
    {
        var users = await _userRepository.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUserById(int id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> AddUser(User user)
    {
        var createdUser = await _userRepository.AddUserAsync(user);
        return CreatedAtAction(nameof(GetUserById), new { id = createdUser.UserID }, createdUser);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, User user)
    {
        if (id != user.UserID)
        {
            return BadRequest();
        }

        await _userRepository.UpdateUserAsync(user);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var deleted = await _userRepository.DeleteUserAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }


    // Get current user profile
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var user = await _userRepository.GetUserByIdAsync(userId);

        if (user == null) return NotFound();

        // Return only allowed fields
        return Ok(new
        {
            user.UserID,
            user.UserName,
            user.Email,
            user.PhoneNumber,
            user.IsAvailable
        });
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateUser([FromBody] User updatedUser)
    {
        var userId = GetCurrentUserId();
        var existingUser = await _userRepository.GetUserByIdAsync(userId);

        // Manual validation
        var validationErrors = new List<string>();

        if (string.IsNullOrWhiteSpace(updatedUser.UserName))
            validationErrors.Add("Username is required");

        if (string.IsNullOrWhiteSpace(updatedUser.Email) || !IsValidEmail(updatedUser.Email))
            validationErrors.Add("Valid email is required");

        if (validationErrors.Any())
            return BadRequest(new { Errors = validationErrors });

        // Only update allowed fields
        existingUser.UserName = updatedUser.UserName;
        existingUser.Email = updatedUser.Email;
        existingUser.PhoneNumber = updatedUser.PhoneNumber;

        // Maintain read-only properties
        existingUser.UpdatedAt = DateTime.UtcNow;
        existingUser.UpdatedBy = userId.ToString();

        await _userRepository.UpdateUserAsync(existingUser);
        return NoContent();
    }

    // Change password
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

        if (!result) return BadRequest("Current password is incorrect");

        return NoContent();
    }




    // Update availability
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