using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using UserService.Data;
using UserService.Dtos;
using UserService.Models;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("users/auth")]
public class AuthController : ControllerBase
{
    private readonly UserDbContext _db;
    private readonly AuthService _auth;

    public AuthController(UserDbContext db, AuthService auth)
    {
        _db = db;
        _auth = auth;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest dto)
    {
        if (await _db.Users.AnyAsync(u => u.Username == dto.Username))
            return BadRequest("Username exists.");

        var user = new User
        {
            Username = dto.Username,
            PasswordHash = dto.Password,
            DisplayName = dto.DisplayName
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { user.Id });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest dto)
    {
        var user = await _auth.Validate(dto.Username, dto.Password);
        if (user == null) return Unauthorized();

        return Ok(new { token = _auth.CreateToken(user) });
    }
}