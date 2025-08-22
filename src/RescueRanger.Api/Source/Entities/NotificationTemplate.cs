using System.ComponentModel.DataAnnotations;

namespace RescueRanger.Api.Entities;

public class NotificationTemplate : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string TemplateName { get; set; } = string.Empty;

    public string SmsBody { get; set; } = string.Empty;
    public string EmailSubject { get; set; } = string.Empty;
    public string EmailBody { get; set; } = string.Empty;
}