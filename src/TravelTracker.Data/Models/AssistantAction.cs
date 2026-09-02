using TravelTracker.Data;

namespace TravelTracker.Data.Models;

[Table("AssistantActions", Schema = DatabaseSchema.Name)]
public sealed class AssistantAction
{
    [Key]
    public Guid Id { get; set; }

    public int UserId { get; set; }

    [MaxLength(200)]
    public string ThreadId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ActionType { get; set; } = "create_location";

    public int CommandSchemaVersion { get; set; } = 1;

    [MaxLength(20)]
    public string State { get; set; } = "Pending";

    [MaxLength(64)]
    public string CanonicalIdempotencyKey { get; set; } = string.Empty;

    public string? CanonicalCommandCiphertext { get; set; }

    [MaxLength(32)]
    public byte[] PayloadHashSha256 { get; set; } = [];

    [MaxLength(400)]
    public string SanitizedSummary { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ErrorCode { get; set; }

    public int? CreatedLocationId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? CompletedDate { get; set; }

    public DateTime RetainUntilDate { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public Location? Location { get; set; }
}
