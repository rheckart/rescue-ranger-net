using System.ComponentModel.DataAnnotations;

namespace RescueRanger.Api.Entities;

public class Member : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public ulong MemberNumber { get; set; }
    public DateOnly SignupDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Designation { get; set; }
    
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    public DateOnly BirthDay { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Gender { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string MobileNumber { get; set; } = string.Empty;
    
    // Address properties (flattened for simplicity)
    [MaxLength(255)]
    public string Street { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string State { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string ZipCode { get; set; } = string.Empty;
    
    // Contact preferences
    public bool Whatsapp { get; set; }
    public bool Viber { get; set; }
    public bool Telegram { get; set; }
}