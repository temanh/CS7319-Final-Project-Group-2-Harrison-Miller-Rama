using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealTimeConnect;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/users
    [HttpGet]
    public IActionResult GetAllUsers()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?? User.FindFirst("nameid")
                ?? User.FindFirst("sub");

            if (userIdClaim == null)
                return Unauthorized(new { message = "User claim not found" });

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return BadRequest(new { message = "Invalid user id claim" });

            var users = _context.Users
                .Where(u => u.Id != userId)
                .Select(u => new { u.Id, u.Username })
                .ToList();
            return Ok(users);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UsersController] GetAllUsers error: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    // GET: api/users/{id}
    [HttpGet("{id}")]
    public IActionResult GetUser(int id)
    {
        var user = _context.Users.Where(u => u.Id == id).Select(u => new { u.Id, u.Username }).FirstOrDefault();
        if (user == null) return NotFound();
        return Ok(user);
    }
}
