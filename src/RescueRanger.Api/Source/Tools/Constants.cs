namespace RescueRanger.Api;

static class Constants
{
    //public const string FileBucketDB = "RescueRanger.Api-FILES";
}

static class NotificationType
{
    public const string MemberWelcome = nameof(MemberWelcome);
    public const string ReviewNewMember = nameof(ReviewNewMember);
}

static class TenantStatus
{
    public const string Provisioning = nameof(Provisioning);
    public const string Active = nameof(Active);
    public const string Inactive = nameof(Inactive);
    public const string Suspended = nameof(Suspended);
    public const string Archived = nameof(Archived);
    public const string PendingDeletion = nameof(PendingDeletion);
}