using Microsoft.EntityFrameworkCore;

namespace RescueRanger.Api.Services;

public class JobStorageProvider : IJobStorageProvider<JobRecord>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public JobStorageProvider(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<IEnumerable<JobRecord>> GetNextBatchAsync(PendingJobSearchParams<JobRecord> p)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var records = await dbContext.JobRecords
            .OrderBy(r => r.Id)
            .Take(p.Limit)
            .ToListAsync(p.CancellationToken);
            
        // Apply the expression filter in memory since EF Core can't translate arbitrary expressions
        return records.Where(p.Match.Compile());
    }

    public async Task MarkJobAsCompleteAsync(JobRecord r, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await dbContext.JobRecords
            .Where(jr => jr.Id == r.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(jr => jr.IsComplete, true), ct);
    }

    public async Task CancelJobAsync(Guid trackingId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await dbContext.JobRecords
            .Where(r => r.TrackingID == trackingId)
            .ExecuteUpdateAsync(s => s.SetProperty(jr => jr.IsComplete, true), ct);
    }

    public async Task OnHandlerExecutionFailureAsync(JobRecord r, Exception exception, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (r.FailureCount > 100)
        {
            r.IsComplete = true;
            r.IsCancelled = true;
            r.CancelledOn = DateTime.UtcNow;
            r.FailureReason = exception.Message;

            dbContext.JobRecords.Update(r);
            await dbContext.SaveChangesAsync(ct);
            return;
        }

        var retryOn = DateTime.UtcNow.AddMinutes(1);
        var expireOn = retryOn.AddHours(4);

        await dbContext.JobRecords
            .Where(jr => jr.Id == r.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(jr => jr.FailureReason, exception.Message)
                .SetProperty(jr => jr.FailureCount, jr => jr.FailureCount + 1)
                .SetProperty(jr => jr.ExecuteAfter, retryOn)
                .SetProperty(jr => jr.ExpireOn, expireOn), ct);
    }

    public async Task PurgeStaleJobsAsync(StaleJobSearchParams<JobRecord> p)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Get records to delete first
        var recordsToDelete = dbContext.JobRecords.Where(p.Match.Compile()).ToList();
        dbContext.JobRecords.RemoveRange(recordsToDelete);
        await dbContext.SaveChangesAsync(p.CancellationToken);
    }

    public async Task StoreJobAsync(JobRecord r, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        dbContext.JobRecords.Add(r);
        await dbContext.SaveChangesAsync(ct);
    }
}