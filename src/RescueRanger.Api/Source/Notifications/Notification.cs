using System.Text;
using System.Text.RegularExpressions;

namespace RescueRanger.Api.Notifications;

public sealed partial class Notification
{
    static readonly Regex _rx = MergeFieldRx();
    static readonly Dictionary<string, NotificationTemplate> _templates = new();

    public static async Task Initialize()
    {
        // TODO: Load templates from EF Core database
        await Task.CompletedTask;
    }

    public string ToName { get; init; } = null!;
    public string ToEmail { get; init; } = null!;
    public string ToMobile { get; init; } = null!;
    public bool SendEmail { get; init; }
    public bool SendSms { get; init; }
    public string Type { get; init; } = null!;

    readonly HashSet<(string Name, string Value)> _mergeFields = [];
    readonly List<string> _missingTags = [];

    public Notification Merge(string fieldName, string fieldValue)
    {
        _mergeFields.Add((fieldName, fieldValue));
        return this;
    }

    public async Task AddToSendingQueueAsync()
    {
        if (string.IsNullOrEmpty(ToName) ||
            (SendEmail && string.IsNullOrEmpty(ToEmail)) ||
            (SendSms && string.IsNullOrEmpty(ToMobile)) ||
            string.IsNullOrEmpty(Type))
            throw new ArgumentNullException(null, "Unable to send notification without all required parameters!");

        // TODO: Implement sending queue using EF Core
        // For now, just log or skip
        await Task.CompletedTask;
    }

    [GeneratedRegex(@"{(\w+)}", RegexOptions.Compiled)]
    private static partial Regex MergeFieldRx();
}

public enum NotificationType
{
    ReviewNewMember,
    WelcomeNewMember
}