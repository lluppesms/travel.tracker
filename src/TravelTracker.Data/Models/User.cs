namespace TravelTracker.Data.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "user";
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EntraIdUserId { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginDate { get; set; }
}
