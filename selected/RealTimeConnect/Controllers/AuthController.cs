using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RealTimeConnect;
using RealTimeConnect.Models.User;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        Console.WriteLine($"[AuthController] Login attempt for username: {request.Username}");
        var user = _context.Users.FirstOrDefault(u => u.Username == request.Username);
        if (user == null)
        {
            Console.WriteLine($"[AuthController] User not found: {request.Username}");
            return Unauthorized();
        }
        var hasher = new PasswordHasher<User>();
        var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed)
        {
            Console.WriteLine($"[AuthController] Password mismatch for user: {request.Username}");
            return Unauthorized();
        }

        var token = GenerateJwtToken(user);
        Console.WriteLine($"[AuthController] Login successful for user: {request.Username} (Id: {user.Id})");
        return Ok(new { token });
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Username and password are required." });

        if (_context.Users.Any(u => u.Username == request.Username))
            return Conflict(new { message = "User already exists." });

        var user = new User
        {
            Username = request.Username
        };

        // Hash the password before storing it
        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        _context.Users.Add(user);
        _context.SaveChanges();

        var token = GenerateJwtToken(user);
        return Ok(new { token });
    }

    private string GenerateJwtToken(User user)
    {
        var keyString = _config["Jwt:Key"];
        if (string.IsNullOrEmpty(keyString))
        {
            throw new InvalidOperationException("Jwt:Key is not configured");
        }

        Console.WriteLine($"[AuthController] Generating token for user: {user.Username} (Id: {user.Id})");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),

            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(claims: claims, expires: DateTime.Now.AddDays(7), signingCredentials: creds);
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        Console.WriteLine($"[AuthController] Token generated successfully");
        return tokenString;
    }
}