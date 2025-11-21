namespace RealTimeConnect.Models.User
{
    public class CreateConversation
    {
        public string Name { get; set; }
        public List<int> MemberIds { get; set; }
    }
}
