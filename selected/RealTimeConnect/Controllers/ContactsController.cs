using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealTimeConnect.Models.User;
using RealTimeConnect;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ContactsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/contacts - list all contacts for current user
    [HttpGet]
    public IActionResult GetContacts()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?? User.FindFirst("nameid")
                ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized(new { message = "Invalid user claim" });

            var contacts = _context.Contacts
                .Where(c => c.OwnerUserId == userId)
                .Select(c => new { c.ContactUserId, c.ContactUser!.Username, c.CreatedAt })
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return Ok(contacts);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ContactsController] GetContacts error: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    // GET: api/contacts/search?query=username - search for users to add as contacts
    [HttpGet("search")]
    public IActionResult SearchUsers([FromQuery] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Query cannot be empty" });

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?? User.FindFirst("nameid")
                ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized(new { message = "Invalid user claim" });

            // Search for users matching query, excluding current user and existing contacts
            var existingContactIds = _context.Contacts
                .Where(c => c.OwnerUserId == userId)
                .Select(c => c.ContactUserId)
                .ToList();

            var results = _context.Users
                .Where(u => u.Id != userId && !existingContactIds.Contains(u.Id))
                .Where(u => u.Username.ToLower().Contains(query.ToLower()))
                .Select(u => new { u.Id, u.Username })
                .OrderBy(u => u.Username)
                .Take(20)
                .ToList();

            return Ok(results);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ContactsController] SearchUsers error: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    // POST: api/contacts - add a new contact
    [HttpPost]
    public IActionResult AddContact([FromBody] AddContactRequest req)
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?? User.FindFirst("nameid")
                ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized(new { message = "Invalid user claim" });

            if (req.ContactUserId <= 0)
                return BadRequest(new { message = "Invalid contact user id" });

            if (userId == req.ContactUserId)
                return BadRequest(new { message = "Cannot add yourself as a contact" });

            // Check if contact already exists
            var existingContact = _context.Contacts
                .Any(c => c.OwnerUserId == userId && c.ContactUserId == req.ContactUserId);

            if (existingContact)
                return BadRequest(new { message = "Contact already exists" });

            // Verify the contact user exists
            var contactUserExists = _context.Users.Any(u => u.Id == req.ContactUserId);
            if (!contactUserExists)
                return BadRequest(new { message = "User not found" });

            var contact = new Contact
            {
                OwnerUserId = userId,
                ContactUserId = req.ContactUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Contacts.Add(contact);
            _context.SaveChanges();

            Console.WriteLine($"[ContactsController] Contact added: {userId} -> {req.ContactUserId}");
            return CreatedAtAction(nameof(GetContacts), new { }, new { userId = contact.OwnerUserId, contactUserId = contact.ContactUserId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ContactsController] AddContact error: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    // DELETE: api/contacts/{contactUserId} - remove a contact
    [HttpDelete("{contactUserId}")]
    public IActionResult RemoveContact(int contactUserId)
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?? User.FindFirst("nameid")
                ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized(new { message = "Invalid user claim" });

            var contact = _context.Contacts
                .FirstOrDefault(c => c.OwnerUserId == userId && c.ContactUserId == contactUserId);

            if (contact == null)
                return NotFound(new { message = "Contact not found" });

            _context.Contacts.Remove(contact);
            _context.SaveChanges();

            Console.WriteLine($"[ContactsController] Contact removed: {userId} -> {contactUserId}");
            return Ok(new { message = "Contact removed" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ContactsController] RemoveContact error: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    public class AddContactRequest
    {
        public int ContactUserId { get; set; }
    }
}
