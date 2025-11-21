namespace RealTimeConnect.Models.User
{
    public class Conversation
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // for group chats
        public bool IsGroup { get; set; } = false;
        public ICollection<ConversationMember> Members { get; set; } = new List<ConversationMember>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();

        public DateTime CreatedAt { get; set; }
    }
}
