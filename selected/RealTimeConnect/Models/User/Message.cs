namespace RealTimeConnect.Models.User
{
    public class Message
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; }

        public int SenderId { get; set; }
        public User Sender { get; set; }

        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
