using RescueRanger.Api.Notifications;

namespace Members.Signup;

sealed class Endpoint : Endpoint<Request, Response, Mapper>
{
    private readonly ApplicationDbContext _dbContext;
    
    public Endpoint(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public override void Configure()
    {
        Post("members/signup");
        PreProcessor<DuplicateInfoChecker>();
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request r, CancellationToken c)
    {
        var member = Map.ToEntity(r);

        // Generate member number (you might want to use a more robust sequence generator)
        var lastMember = await _dbContext.Members
            .OrderByDescending(m => m.MemberNumber)
            .FirstOrDefaultAsync(c);
        
        member.MemberNumber = (lastMember?.MemberNumber ?? 100) + 1;
        member.SignupDate = DateOnly.FromDateTime(DateTime.UtcNow);

        _dbContext.Members.Add(member);
        await _dbContext.SaveChangesAsync(c);

        //todo: send email to member

        await new Notification
            {
                Type = RescueRanger.Api.Notifications.NotificationType.ReviewNewMember.ToString(),
                SendEmail = true,
                SendSms = false,
                ToEmail = Config["Email:Administrator"]!,
                ToName = "RescueRanger.Api Admin"
            }.Merge("{MemberName}", $"{member.FirstName} {member.LastName}")
             .Merge("{LoginLink}", "https://RescueRanger.Api.com/admin/login")
             .Merge("{TrackingId}", member.Id.ToString())
             .AddToSendingQueueAsync();

        Response = new()
        {
            MemberId = member.Id.ToString(),
            MemberNumber = member.MemberNumber
        };
    }
}