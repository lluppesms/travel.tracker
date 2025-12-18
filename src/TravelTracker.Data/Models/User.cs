namespace TravelTracker.Data.Models;

[Table("Users")]
//[Index(nameof(ApiKey), IsUnique = true)]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [MaxLength(50)]
    public string Type { get; set; } = "user";

    [Required]
    [MaxLength(200)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string EntraIdUserId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ApiKey { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginDate { get; set; }
}
