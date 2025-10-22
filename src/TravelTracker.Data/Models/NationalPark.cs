using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelTracker.Data.Models;

[Table("NationalParks")]
public class NationalPark
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [MaxLength(50)]
    public string Type { get; set; } = "nationalpark";
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string State { get; set; } = string.Empty;
    
    public double Latitude { get; set; }
    
    public double Longitude { get; set; }
    
    public string Description { get; set; } = string.Empty;
}
