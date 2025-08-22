namespace Members.Signup;

sealed class DuplicateInfoChecker : IPreProcessor<Request>
{
    private readonly ApplicationDbContext _dbContext;
    
    public DuplicateInfoChecker(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task PreProcessAsync(IPreProcessorContext<Request> ctx, CancellationToken ct)
    {
        var emailTask = _dbContext.Members
            .AnyAsync(m => m.Email == ctx.Request.Email.ToLower(), ct);
            
        var mobileTask = _dbContext.Members
            .AnyAsync(m => m.MobileNumber == ctx.Request.Contact.MobileNumber.Trim(), ct);

        await Task.WhenAll(emailTask, mobileTask);

        if (emailTask.Result)
            ctx.ValidationFailures.Add(new(nameof(Request.Email), "Email address is in use!"));
            
        if (mobileTask.Result)
            ctx.ValidationFailures.Add(new($"{nameof(Request.Contact)}.{nameof(Request.Contact.MobileNumber)}", "Mobile number is in use!"));

        if (ctx.ValidationFailures.Count > 0)
            await ctx.HttpContext.Response.SendErrorsAsync(ctx.ValidationFailures, cancellation: ct);
    }
}