using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Restaurant_Menu.Models;
using Restaurant_Menu.Interface;

namespace Restaurant_Menu.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _config;
        private readonly PasswordHasher<User> _hasher = new();

        public AuthController(IUserRepository userRepo, IConfiguration config)
        {
            _userRepository = userRepo;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Dictionary<string, string> creds)
        {
            if (!creds.ContainsKey("email") || !creds.ContainsKey("password"))
                return BadRequest("Email and password are required.");

            var email = creds["email"];
            var password = creds["password"];

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || !user.IsAvailable)
                return Unauthorized("Invalid credentials.");

            // Restaurant validation - ensure user belongs to the restaurant they're trying to access
            if (creds.ContainsKey("restaurantId") && int.TryParse(creds["restaurantId"], out int requestedRestaurantId))
            {
                if (user.RestaurantID != requestedRestaurantId)
                {
                    return Unauthorized($"User not authorized for restaurant {requestedRestaurantId}");
                }
            }

            bool isPlainTextPassword = string.IsNullOrEmpty(user.PasswordHash) ||
                                      !user.PasswordHash.StartsWith("AQAAAA");

            if (isPlainTextPassword)
            {
                if (user.PasswordHash == password)
                {
                    user.PasswordHash = _hasher.HashPassword(user, password);
                    await _userRepository.UpdateUserAsync(user);
                }
                else
                {
                    return Unauthorized("Invalid credentials.");
                }
            }
            else
            {
                var verifyResult = _hasher.VerifyHashedPassword(user, user.PasswordHash!, password);
                if (verifyResult == PasswordVerificationResult.Failed)
                    return Unauthorized("Invalid credentials.");
            }

            // Generate JWT with restaurant claim
            var jwtSection = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
            var credsSigning = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
        new Claim(ClaimTypes.Role, user.UserRole),
        new Claim("restaurantId", user.RestaurantID.ToString()),
        new Claim(ClaimTypes.Email, user.Email) // Add email to claims
    };

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSection["DurationInMinutes"]!)),
                signingCredentials: credsSigning
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                token = tokenString,
                userId = user.UserID,
                restaurantId = user.RestaurantID,
                role = user.UserRole,
                email = user.Email
            });
        }
    }
}
