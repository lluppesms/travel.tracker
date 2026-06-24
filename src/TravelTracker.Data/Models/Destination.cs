using TravelTracker.Data;

namespace TravelTracker.Data.Models;

[Table("Destinations", Schema = DatabaseSchema.Name)]
public class Destination
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DestinationTypeId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string State { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string Description { get; set; } = string.Empty;

    // Navigation property
    public DestinationType? DestinationType { get; set; }
}
