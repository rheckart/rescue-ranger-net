namespace RescueRanger.Api.Entities;

/// <summary>
/// Base class for all entities that belong to a specific tenant
/// </summary>
public abstract class TenantEntity : BaseEntity, ITenantEntity
{
    /// <summary>
    /// The unique identifier of the tenant this entity belongs to
    /// </summary>
    public Guid TenantId { get; set; }
}

/// <summary>
/// Base entity with common audit fields
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// When the entity was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// User who created the entity
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// When the entity was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// User who last updated the entity
    /// </summary>
    public string? UpdatedBy { get; set; }
    
    /// <summary>
    /// Row version for optimistic concurrency
    /// </summary>
    public byte[]? RowVersion { get; set; }
}