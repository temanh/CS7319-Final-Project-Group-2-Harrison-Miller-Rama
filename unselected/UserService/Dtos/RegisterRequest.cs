namespace UserService.Dtos
{
    public class RegisterRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string? DisplayName { get; set; }
    }
}
