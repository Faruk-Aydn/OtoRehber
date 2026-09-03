using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace OtoRehber.Tests;

/// <summary>
/// AI Sihirbaz uçtan uca (PRD v5 §4.3): AI yapılandırılmamış olsa bile backend
/// kural motoru adayları üretmeli ve Result sayfası render olmalı.
/// </summary>
public class AiWizardFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AiWizardFlowTests(CustomWebApplicationFactory factory)
        => _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

    private async Task<string> GetAntiforgeryTokenAsync(string url)
    {
        var html = await _client.GetStringAsync(url);
        var m = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        Assert.True(m.Success, "Antiforgery token not found on " + url);
        return m.Groups[1].Value;
    }

    [Fact]
    public async Task Analyze_WithBudget_RendersBackendCandidates_EvenWithoutAi()
    {
        var token = await GetAntiforgeryTokenAsync("/AiWizard");

        var res = await _client.PostAsync("/AiWizard/Analyze", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["budgetMin"] = "500000",
            ["budgetMax"] = "1500000",
        }));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        // HasData: Golf (800k-1.2M) ve Corolla (700k-1.1M) bütçeye uyuyor → aday kartları.
        // (Razor Türkçe karakterleri &#xNN; olarak encode eder — ASCII parçalarla assert et.)
        Assert.Contains("OtoRehber Skoru", body);
        Assert.Contains("Corolla", body);
        Assert.Contains("AI yorumu", body);
    }

    [Fact]
    public async Task Analyze_ImpossibleBudget_ShowsNoMatchMessage()
    {
        var token = await GetAntiforgeryTokenAsync("/AiWizard");

        var res = await _client.PostAsync("/AiWizard/Analyze", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["budgetMax"] = "50000",
        }));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("Kriterleri d", body);   // "Kriterleri düzenle" butonu
        Assert.DoesNotContain("AI yorumu", body); // aday yoksa AI bloğu render edilmez
    }

    [Fact]
    public async Task Compare_Result_ShowsBackendWinner()
    {
        // Golf (Id=1, skor ~5.3) vs Corolla (Id=2, skor ~9.7) → Corolla öne çıkar
        var body = await _client.GetStringAsync("/Compare/Result?car1Id=1&car2Id=2");
        Assert.Contains("Kazanan backend", body);       // banner açıklaması (ASCII kısmı)
        Assert.Contains("Toyota Corolla", body);         // öne çıkan araç etiketi
        Assert.Contains("canonical OtoRehber Skoru", body);
    }
}
