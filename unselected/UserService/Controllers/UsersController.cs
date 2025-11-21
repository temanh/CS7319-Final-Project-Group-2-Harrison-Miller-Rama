using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using UserService.Data;

namespace UserService.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly UserDbContext _db;

    public UsersController(UserDbContext db)
    {
        _db = db;
    }

    [HttpGet("profile/{id}")]
    public async Task<IActionResult> Profile(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        return Ok(new { user.Id, user.Username, user.DisplayName });
    }
}