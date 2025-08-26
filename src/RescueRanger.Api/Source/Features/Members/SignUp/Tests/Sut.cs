using Amazon.SimpleEmailV2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Configuration;
namespace Members.Signup.Tests;

public class Sut : AppFixture<Program>
{
    internal Request SignupRequest { get; set; } = default!;
    internal Guid? MemberId { get; set; }
    private ApplicationDbContext? _dbContext;

    protected override void ConfigureApp(IWebHostBuilder a)
    {
        a.UseContentRoot(Directory.GetCurrentDirectory());
        a.UseEnvironment("Testing");
        
        // Override the database configuration at the WebHost level
        a.ConfigureServices(services =>
        {
            // Remove existing DbContext configuration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);
                
            var dbContextOptionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptionsBuilder<ApplicationDbContext>));
            if (dbContextOptionsDescriptor != null)
                services.Remove(dbContextOptionsDescriptor);

            // Add InMemory database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                options.EnableSensitiveDataLogging();
            });
            
            // Override the SES client
            services.AddSingleton<IAmazonSimpleEmailServiceV2, SesClient>();
            
            // Override configuration to provide Redis connection string
            var testConfig = new Dictionary<string, string>
            {
                ["ConnectionStrings:Redis"] = "localhost:6379", // Dummy connection string for testing
                ["Environment"] = "Testing"
            };
            
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(testConfig!)
                .Build();
            
            services.AddSingleton<IConfiguration>(config);
        });
    }

    protected override async ValueTask SetupAsync()
    {
        _dbContext = Services.GetRequiredService<ApplicationDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();

        SignupRequest = new()
        {
            UserDetails = new()
            {
                FirstName = Fake.Name.FirstName(),
                LastName = Fake.Name.LastName()
            },
            Address = new()
            {
                City = Fake.Address.City(),
                State = Fake.Address.State(),
                ZipCode = Fake.Address.ZipCode("#####"),
                Street = Fake.Address.StreetAddress()
            },
            BirthDay = "1983-10-10",
            Contact = new()
            {
                MobileNumber = Fake.Phone.PhoneNumber("##########"),
                Telegram = true,
                Whatsapp = true
            },
            Email = Fake.Internet.Email(),
            Gender = "Male"
        };
    }

    protected override async ValueTask TearDownAsync()
    {
        if (_dbContext != null)
        {
            if (MemberId.HasValue)
            {
                var member = await _dbContext.Members.FindAsync(MemberId.Value);
                if (member != null)
                {
                    _dbContext.Members.Remove(member);
                    await _dbContext.SaveChangesAsync();
                }
            }
            
            await _dbContext.DisposeAsync();
        }
    }
}