namespace RescueRanger.Api.Entities;

/// <summary>
/// Interface for entities that belong to a specific tenant
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// The unique identifier of the tenant this entity belongs to
    /// </summary>
    Guid TenantId { get; set; }
}