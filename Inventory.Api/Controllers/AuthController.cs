using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using inventory_management.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly IConfiguration _config;

        public AuthController(IAuthenticationService authService, IConfiguration config)
        {
            _authService = authService;
            _config = config;
        }

        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request.Username, request.Password);
            
            if (!result.Success)
            {
                return Unauthorized(new { message = result.Message });
            }

            var token = GenerateJwtToken(result.User!.Username);

            return Ok(new
            {
                token = token,
                username = result.User.Username
            });
        }

        private string GenerateJwtToken(string username)
        {
            var jwtKey = _config["Jwt:Key"] ?? "DefaultSecretKeyThatIsAtLeast32BytesLong!";
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
