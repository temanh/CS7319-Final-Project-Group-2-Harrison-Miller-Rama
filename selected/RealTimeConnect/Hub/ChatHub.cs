using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RealTimeConnect.Models.User;

namespace RealTimeConnect.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(int conversationId, string message)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var msg = new Message
            {
                ConversationId = conversationId,
                SenderId = userId,
                Content = message,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
            {
                conversationId,
                senderId = userId,
                message,
                timestamp = msg.SentAt
            });
        }

        public override async Task OnConnectedAsync()
        {
            var userId = int.Parse(Context.UserIdentifier!);
            var userConversations = _context.ConversationMembers
                .Where(c => c.UserId == userId)
                .Select(c => c.ConversationId)
                .ToList();

            foreach (var convoId in userConversations)
                await Groups.AddToGroupAsync(Context.ConnectionId, convoId.ToString());

            Console.WriteLine($"[ChatHub] ConnectionId: {Context.ConnectionId}, User: {Context.UserIdentifier}");

            await base.OnConnectedAsync();
        }
    }
}