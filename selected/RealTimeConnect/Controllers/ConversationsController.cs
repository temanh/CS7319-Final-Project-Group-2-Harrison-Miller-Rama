using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealTimeConnect.Models.User;
using RealTimeConnect;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ConversationsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/conversations
    [HttpGet]
    public IActionResult GetMyConversations()
    {
        try
        {
            // Extract userId from claims (try multiple possible claim types)
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?? User.FindFirst("nameid")
                ?? User.FindFirst("sub");

            if (userIdClaim == null)
            {
                Console.WriteLine($"[ConversationsController] No user id claim found. Available claims: {string.Join(", ", User.Claims.Select(c => c.Type))}");
                return Unauthorized(new { message = "User claim not found" });
            }

            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                return BadRequest(new { message = "Invalid user id claim" });
            }

            Console.WriteLine($"[ConversationsController] GetMyConversations for userId: {userId}");

            var convos = _context.Conversations
                .Where(c => c.Members.Any(m => m.UserId == userId))
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    Members = c.Members.Select(m => new { m.UserId, m.User.Username }),
                    LastMessage = c.Messages.OrderByDescending(m => m.SentAt).Select(m => new { m.Id, m.Content, m.SentAt }).FirstOrDefault()
                })
                .ToList();

            return Ok(convos);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ConversationsController] GetMyConversations error: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    public class CreateConversationRequest
    {
        public string Name { get; set; } = string.Empty;
        public int[] MemberIds { get; set; } = new int[0];
    }

    // POST: api/conversations
    [HttpPost]
    public IActionResult CreateConversation([FromBody] CreateConversationRequest req)
    {
        try
        {
            Console.WriteLine($"[ConversationsController] CreateConversation called with Name: {req.Name}, MemberIds: {string.Join(",", req.MemberIds)}");

            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { message = "Conversation name is required" });

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?? User.FindFirst("nameid")
                ?? User.FindFirst("sub");

            if (userIdClaim == null)
            {
                Console.WriteLine($"[ConversationsController] User claim not found");
                return Unauthorized(new { message = "User claim not found" });
            }

            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                Console.WriteLine($"[ConversationsController] Invalid user id claim: {userIdClaim.Value}");
                return BadRequest(new { message = "Invalid user id claim" });
            }

            Console.WriteLine($"[ConversationsController] UserId from claim: {userId}");

            if (req.MemberIds == null || req.MemberIds.Length < 1)
            {
                Console.WriteLine($"[ConversationsController] Invalid MemberIds count: {(req.MemberIds == null ? 0 : req.MemberIds.Length)}");
                return BadRequest(new { message = "Conversations require at least one other member in MemberIds." });
            }

            // Build final member list (distinct) and ensure caller is included
            var ids = req.MemberIds.Distinct().ToList();
            if (!ids.Contains(userId)) ids.Add(userId);

            Console.WriteLine($"[ConversationsController] Final members: {string.Join(",", ids)}");

            // If this is a one-on-one chat, check for existing convo between the two users and prevent duplicates
            if (ids.Count == 2)
            {
                var otherUserId = ids.First(i => i != userId);
                var existingConvo = _context.Conversations
                    .Where(c => c.Members.Count == 2)
                    .Where(c => c.Members.Any(m => m.UserId == userId) && c.Members.Any(m => m.UserId == otherUserId))
                    .FirstOrDefault();

                if (existingConvo != null)
                {
                    Console.WriteLine($"[ConversationsController] Conversation already exists between users {userId} and {otherUserId}. Id: {existingConvo.Id}");
                    return BadRequest(new { message = "Conversation already exists with this user" });
                }
            }

            var convo = new Conversation
            {
                Name = req.Name
            };

            foreach (var id in ids)
            {
                convo.Members.Add(new ConversationMember { UserId = id });
            }

            _context.Conversations.Add(convo);
            _context.SaveChanges();

            Console.WriteLine($"[ConversationsController] Conversation created with Id: {convo.Id}");
            return CreatedAtAction(nameof(GetMessages), new { id = convo.Id }, new { convo.Id, convo.Name });
        }
        catch (Exception ex)
        {
            string innerMsg = ex.InnerException?.Message ?? "No inner exception";
            Console.WriteLine($"[ConversationsController] CreateConversation error: {ex.Message}");
            Console.WriteLine($"[ConversationsController] Inner exception: {innerMsg}");
            Console.WriteLine($"[ConversationsController] StackTrace: {ex.StackTrace}");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message, innerError = innerMsg });
        }
    }

    // GET: api/conversations/{id}/messages
    [HttpGet("{id}/messages")]
    public IActionResult GetMessages(int id, int skip = 0, int take = 100)
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

            var isMember = _context.ConversationMembers.Any(cm => cm.ConversationId == id && cm.UserId == userId);
            if (!isMember) return Forbid();

            var msgs = _context.Messages
                .Where(m => m.ConversationId == id)
                .OrderBy(m => m.SentAt)
                .Skip(skip)
                .Take(take)
                .Select(m => new { m.Id, m.SenderId, Sender = m.Sender.Username, m.Content, m.SentAt })
                .ToList();

            return Ok(msgs);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ConversationsController] GetMessages error: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }
}
