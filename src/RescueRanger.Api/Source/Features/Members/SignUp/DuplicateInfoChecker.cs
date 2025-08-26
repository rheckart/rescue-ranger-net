namespace Members.Signup;

sealed class DuplicateInfoChecker(IServiceScopeFactory scopeFactory) : IPreProcessor<Request>
{
    public async Task PreProcessAsync(IPreProcessorContext<Request> ctx, CancellationToken ct)
    {
        if (ctx.Request is null)
        {
            ctx.ValidationFailures.Add(new(nameof(Request), "Request is null!"));
            await ctx.HttpContext.Response.SendErrorsAsync(ctx.ValidationFailures, cancellation: ct);
            return;
        }
        
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var emailTask = dbContext.Members
            .AnyAsync(m => m.Email == ctx.Request.Email.ToLower(), ct);
            
        var mobileTask = dbContext.Members
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