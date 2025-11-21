using ChatService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ChatService.Controllers;

[ApiController]
[Route("chat/messages")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly MessageDbContext _db;

    public MessagesController(MessageDbContext db)
    {
        _db = db;
    }

    [HttpGet("history/{otherUserId}")]
    public async Task<IActionResult> History(int otherUserId)
    {
        var userId = int.Parse(User.FindFirstValue("sub"));

        var messages = await _db.Messages
            .Where(m =>
                (m.FromUserId == userId && m.ToUserId == otherUserId) ||
                (m.FromUserId == otherUserId && m.ToUserId == userId))
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return Ok(messages);
    }
}