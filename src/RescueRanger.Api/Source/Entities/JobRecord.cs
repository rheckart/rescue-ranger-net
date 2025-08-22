using MessagePack;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RescueRanger.Api.Entities;

public class JobRecord : BaseEntity, IJobStorageRecord
{
    [Required]
    public string QueueID { get; set; } = string.Empty;

    public Guid TrackingID { get; set; }

    public DateTime ExecuteAfter { get; set; }
    public DateTime ExpireOn { get; set; }
    public bool IsComplete { get; set; }
    public int FailureCount { get; set; }

    public string? FailureReason { get; set; }
    public bool? IsCancelled { get; set; }
    public DateTime? CancelledOn { get; set; }

    static JobRecord()
    {
        MessagePackSerializer.DefaultOptions = MessagePack.Resolvers.ContractlessStandardResolver.Options;
    }

    // Store serialized command as byte array in database
    private byte[] _commandBytes = [];
    
    [NotMapped]
    public object Command 
    { 
        get => _commandBytes;
        set => _commandBytes = (byte[])value;
    }

    // Store command bytes in database
    public byte[] CommandBytes 
    { 
        get => _commandBytes;
        set => _commandBytes = value;
    }

    void IJobStorageRecord.SetCommand<TCommand>(TCommand command)
    {
        _commandBytes = MessagePackSerializer.Serialize(command);
    }

    TCommand IJobStorageRecord.GetCommand<TCommand>()
    {
        return MessagePackSerializer.Deserialize<TCommand>(_commandBytes);
    }

    // Ignore attribute is MongoDB-specific, removed
    [NotMapped]
    public bool DeleteAfterSuccess => true;

    [NotMapped]
    public bool DeleteAfterFailure => false;

    [NotMapped]
    public bool DeleteAfterCancellation => true;

    [NotMapped]
    public bool DeleteAfterExpiry => true;
}