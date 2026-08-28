using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace OtoRehber.Tests;

public class SmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SmokeTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Stats")]
    [InlineData("/Compare")]
    [InlineData("/Account/Login")]
    [InlineData("/Account/Register")]
    [InlineData("/Account/ForgotPassword")]
    [InlineData("/Home/Kvkk")]
    [InlineData("/Home/Privacy")]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/manifest.json")]
    [InlineData("/service-worker.js")]
    [InlineData("/offline.html")]
    public async Task Get_PublicPages_ReturnsSuccess(string url)
    {
        var res = await _client.GetAsync(url);
        Assert.True(res.IsSuccessStatusCode, $"{url} → {(int)res.StatusCode}");
    }

    [Fact]
    public async Task Get_CarDetails_SeededCar_ReturnsSuccess()
    {
        // OnModelCreating.HasData ile Golf (Id=1) seed'lenir.
        var res = await _client.GetAsync("/Car/Details/1");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Get_AdminArea_Anonymous_RedirectsToLogin()
    {
        var res = await _client.GetAsync("/AdminCar");
        Assert.Equal(HttpStatusCode.Found, res.StatusCode);
        Assert.Contains("/Account/Login", res.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task Post_WithoutAntiforgeryToken_IsRejected()
    {
        var res = await _client.PostAsync("/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "x@example.com",
                ["Password"] = "whatever"
            }));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
