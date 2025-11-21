using RealTimeConnect.Models.User;

public class Contact
{
    public int OwnerUserId { get; set; }
    public User OwnerUser { get; set; }

    public int ContactUserId { get; set; }
    public User ContactUser { get; set; }

    public DateTime CreatedAt { get; set; }
}