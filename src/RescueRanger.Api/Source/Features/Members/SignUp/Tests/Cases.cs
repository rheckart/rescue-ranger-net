using System.Net;
using Amazon.SimpleEmailV2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Members.Signup.Tests;

public class Cases(Sut App) : TestBase<Sut>
{
    [Fact]
    public async Task Invalid_User_Input()
    {
        var req = new Request
        {
            UserDetails = new()
            {
                FirstName = "aa",
                LastName = "bb"
            },
            Email = "badmail.cc",
            BirthDay = "2020-10-10",
            Gender = "nada",
            Contact = new() { MobileNumber = "12345" },
            Address = new()
            {
                City = "c",
                Street = "s"
            }
        };

        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, ErrorResponse>(req);

        rsp.IsSuccessStatusCode.ShouldBeFalse();

        var errKeys = res.Errors.Keys.ToList();
        errKeys.ShouldBe(
        [
            "userDetails.FirstName",
            "userDetails.LastName",
            "email",
            "birthDay",
            "gender",
            "contact.MobileNumber",
            "address.State",
            "address.ZipCode"
        ]);
    }

    [Fact, Priority(1)]
    public async Task Successful_Member_Creation()
    {
        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, Response>(App.SignupRequest);

        rsp.IsSuccessStatusCode.ShouldBeTrue();
        Guid.TryParse(res.MemberId, out var memberId).ShouldBeTrue();
        App.MemberId = memberId;
        res.MemberNumber.ShouldBeOfType<ulong>();
        res.MemberNumber.ShouldBeGreaterThan(0UL);

        var dbContext = App.Services.GetRequiredService<ApplicationDbContext>();
        var actual = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == App.MemberId);

        actual.ShouldNotBeNull();
        actual.City.ShouldBe(App.SignupRequest.Address.City.TitleCase());
        actual.State.ShouldBe(App.SignupRequest.Address.State.TitleCase());
        actual.ZipCode.ShouldBe(App.SignupRequest.Address.ZipCode.Trim());
        actual.Street.ShouldBe(App.SignupRequest.Address.Street.Trim());
        actual.BirthDay.ShouldBe(DateOnly.Parse(App.SignupRequest.BirthDay));
        actual.Email.ShouldBe(App.SignupRequest.Email.LowerCase());
        actual.FirstName.ShouldBe(App.SignupRequest.UserDetails.FirstName.TitleCase());
        actual.Gender.ShouldBe(App.SignupRequest.Gender.TitleCase());
        actual.LastName.ShouldBe(App.SignupRequest.UserDetails.LastName.TitleCase());
        actual.MemberNumber.ShouldBe(res.MemberNumber);
        actual.SignupDate.ShouldBe(DateOnly.FromDateTime(DateTime.UtcNow));
        actual.MobileNumber.ShouldBe(App.SignupRequest.Contact.MobileNumber.Trim());

        var fakeSesClient = (SesClient)App.Services.GetRequiredService<IAmazonSimpleEmailServiceV2>();
        (await fakeSesClient.EmailReceived(App.MemberId.ToString())).ShouldBeTrue();
    }

    [Fact, Priority(2)]
    public async Task Duplicate_Info_Validation()
    {
        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, ErrorResponse>(App.SignupRequest);

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errKeys = res.Errors.Keys.ToList();
        errKeys.ShouldBe(["Email", "Contact.MobileNumber"]);
    }
}