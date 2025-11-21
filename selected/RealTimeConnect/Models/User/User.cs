using System.ComponentModel.DataAnnotations;

namespace RealTimeConnect.Models.User
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public ICollection<ConversationMember> Conversations { get; set; } = new List<ConversationMember>();
    }
}
