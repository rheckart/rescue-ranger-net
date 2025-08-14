namespace RescueRanger.Core.Entities;

/// <summary>
/// Represents a horse in the rescue system
/// </summary>
public class Horse : TenantEntity
{
    /// <summary>
    /// Name of the horse
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Breed of the horse
    /// </summary>
    public string? Breed { get; set; }
    
    /// <summary>
    /// Age of the horse in years
    /// </summary>
    public int? Age { get; set; }
    
    /// <summary>
    /// Color/marking description
    /// </summary>
    public string? Color { get; set; }
    
    /// <summary>
    /// Gender of the horse
    /// </summary>
    public string? Gender { get; set; }
    
    /// <summary>
    /// Height in hands
    /// </summary>
    public decimal? HeightHands { get; set; }
    
    /// <summary>
    /// Weight in pounds
    /// </summary>
    public decimal? WeightPounds { get; set; }
    
    /// <summary>
    /// Microchip number if applicable
    /// </summary>
    public string? MicrochipNumber { get; set; }
    
    /// <summary>
    /// Current status (Available, Adopted, In Care, etc.)
    /// </summary>
    public string Status { get; set; } = "In Care";
    
    /// <summary>
    /// Date the horse arrived at the rescue
    /// </summary>
    public DateTime ArrivalDate { get; set; }
    
    /// <summary>
    /// Medical notes and history
    /// </summary>
    public string? MedicalNotes { get; set; }
    
    /// <summary>
    /// Behavioral notes and training history
    /// </summary>
    public string? BehavioralNotes { get; set; }
    
    /// <summary>
    /// Special care requirements
    /// </summary>
    public string? SpecialNeeds { get; set; }
    
    /// <summary>
    /// Whether the horse is available for adoption
    /// </summary>
    public bool IsAvailableForAdoption { get; set; }
    
    /// <summary>
    /// Adoption fee if applicable
    /// </summary>
    public decimal? AdoptionFee { get; set; }
    
    /// <summary>
    /// Photos of the horse (URLs or paths)
    /// </summary>
    public List<string> PhotoUrls { get; set; } = new();
    
    /// <summary>
    /// Location within the facility
    /// </summary>
    public string? CurrentLocation { get; set; }
}