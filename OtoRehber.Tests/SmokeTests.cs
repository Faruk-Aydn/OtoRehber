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
    [InlineData("/robots.txt")]
    [InlineData("/sitemap.xml")]
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
    public async Task Sitemap_ListsSeededCarDetailUrls()
    {
        var xml = await _client.GetStringAsync("/sitemap.xml");
        Assert.Contains("<loc>", xml);
        Assert.Contains("/Car/Details/1</loc>", xml);
        Assert.Contains("/marka/", xml);
        Assert.Contains("/segment/", xml);
    }

    [Theory]
    [InlineData("/marka/volkswagen")]
    [InlineData("/marka/toyota")]
    [InlineData("/segment/c")]
    public async Task CatalogPages_SeededSlugs_ReturnSuccess(string url)
    {
        var res = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Catalog_UnknownSlug_Returns404()
    {
        var res = await _client.GetAsync("/marka/boyle-bir-marka-yok");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task CarDetails_EmitsJsonLd()
    {
        var html = await _client.GetStringAsync("/Car/Details/1");
        Assert.Contains("application/ld+json", html);
        Assert.Contains("\"@type\":\"Car\"", html);
    }

    [Theory]
    [InlineData("/AdminCar")]
    [InlineData("/Manage")]
    [InlineData("/Manage/DeleteAccount")]
    [InlineData("/Garage")]
    public async Task Get_ProtectedArea_Anonymous_RedirectsToLogin(string url)
    {
        var res = await _client.GetAsync(url);
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

    [Fact]
    public async Task ToggleReviewLike_Anonymous_RedirectsToLogin()
    {
        var res = await _client.PostAsync("/Car/ToggleReviewLike",
            new StringContent("{\"reviewId\":1}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Found, res.StatusCode);
        Assert.Contains("/Account/Login", res.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task CarDetails_RendersReviewSection()
    {
        var html = await _client.GetStringAsync("/Car/Details/1");
        Assert.Contains("Topluluk Yorumları", html);
    }
}
