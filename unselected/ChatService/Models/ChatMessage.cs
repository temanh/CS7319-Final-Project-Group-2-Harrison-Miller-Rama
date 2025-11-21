using System;

namespace ChatService.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public string Content { get; set; } = "";
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
