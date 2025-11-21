using ChatService.Data;
using ChatService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ChatService.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly MessageDbContext _db;

    public ChatHub(MessageDbContext db)
    {
        _db = db;
    }

    public async Task SendMessage(int toUserId, string message)
    {
        var fromUserId = int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier) ??
                                   Context.User!.FindFirstValue("sub"));

        var msg = new ChatMessage
        {
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Content = message
        };

        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();

        await Clients.User(toUserId.ToString())
            .SendAsync("ReceiveMessage", msg);
    }
}