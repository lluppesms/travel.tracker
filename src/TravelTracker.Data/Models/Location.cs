using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelTracker.Data.Models;

[Table("Locations")]
public class Location
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [MaxLength(50)]
    public string Type { get; set; } = "location";
    
    [Required]
    [MaxLength(50)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string LocationType { get; set; } = string.Empty;
    
    [MaxLength(300)]
    public string Address { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string State { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string ZipCode { get; set; } = string.Empty;
    
    public double Latitude { get; set; }
    
    public double Longitude { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    public int Rating { get; set; }
    
    public string Comments { get; set; } = string.Empty;
    
    [NotMapped]
    public List<string> Tags { get; set; } = new();
    
    [MaxLength(2000)]
    public string TagsJson { get; set; } = "[]";
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}
