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

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User newUser)
        {
            // whitelist allowed roles
            var role = newUser.UserRole?.Trim();
            if (role != "Waiter" && role != "Customer" && role != "Kitchen")
                return BadRequest("Invalid role");

            newUser.UserRole = role;
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.UpdatedAt = DateTime.UtcNow;
            newUser.PasswordHash = _hasher.HashPassword(newUser, newUser.PasswordHash!);

            var created = await _userRepository.AddUserAsync(newUser);
            return CreatedAtAction(null, new { id = created.UserID }, new { created.UserID });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Dictionary<string, string> creds)
        {
            if (!creds.ContainsKey("email") || !creds.ContainsKey("password"))
                return BadRequest("Email & password required");

            var user = await _userRepository.GetByEmailAsync(creds["email"]);
            if (user == null) return Unauthorized();

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash!, creds["password"]);
            if (result == PasswordVerificationResult.Failed) return Unauthorized();

            var jwtSettings = _config.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDesc = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                    new Claim(ClaimTypes.Role,           user.UserRole)
                }),
                Expires = DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["DurationInMinutes"])),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDesc);
            return Ok(new { token = tokenHandler.WriteToken(token) });
        }
    }
}
