using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OtoRehber.Infrastructure.Services;
using Xunit;

namespace OtoRehber.Tests;

public class AiCarDataServiceTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;
        public HttpRequestMessage? LastRequest { get; private set; }

        public StubHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body)
            });
        }
    }

    private static AiCarDataService CreateService(HttpMessageHandler handler, string? apiKey)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["GeminiApiKey"] = apiKey })
            .Build();

        var scopeFactory = new ServiceCollection().BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();

        return new AiCarDataService(
            NullLogger<AiCarDataService>.Instance,
            scopeFactory,
            config,
            new HttpClient(handler));
    }

    private static readonly Dictionary<string, int> NoRefs = new();

    [Fact]
    public async Task Answer_NoApiKey_ReturnsFailWithMessage()
    {
        var svc = CreateService(new StubHandler(HttpStatusCode.OK, "{}"), apiKey: "");

        var result = await svc.AnswerQuestionAsync("Golf nasıl?", null, NoRefs, NoRefs);

        Assert.False(result.Ok);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public async Task Answer_StructuredResponse_ParsesSummary_AndRejectsUnknownClaims()
    {
        // Gemini responseMimeType=application/json → parts[0].text bir JSON string.
        const string inner = "{\"summary\":\"CVT şanzımana dikkat.\",\"claims\":[{\"type\":\"known_issue\",\"referenceId\":\"issue-999\"}]}";
        var geminiJson = $$"""
        { "candidates": [ { "content": { "parts": [ { "text": {{System.Text.Json.JsonSerializer.Serialize(inner)}} } ] } } ] }
        """;
        var handler = new StubHandler(HttpStatusCode.OK, geminiJson);
        var svc = CreateService(handler, apiKey: "test-key-123");

        var result = await svc.AnswerQuestionAsync("CVT sorunu var mı?", "ARAÇ #1 ...", NoRefs, NoRefs);

        Assert.True(result.Ok);
        Assert.Equal("CVT şanzımana dikkat.", result.Summary);
        Assert.Equal(0, result.AcceptedClaims);
        Assert.Equal(1, result.RejectedClaims); // issue-999 bağlamda yok → REJECT
        Assert.Contains("test-key-123", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task Answer_ValidClaim_IsAccepted()
    {
        const string inner = "{\"summary\":\"x\",\"claims\":[{\"type\":\"maintenance\",\"referenceId\":\"maint-5\"}]}";
        var geminiJson = $$"""
        { "candidates": [ { "content": { "parts": [ { "text": {{System.Text.Json.JsonSerializer.Serialize(inner)}} } ] } } ] }
        """;
        var svc = CreateService(new StubHandler(HttpStatusCode.OK, geminiJson), apiKey: "k");

        var result = await svc.AnswerQuestionAsync("q", "ctx", NoRefs, new Dictionary<string, int> { ["maint-5"] = 0 });

        Assert.True(result.Ok);
        Assert.Equal(1, result.AcceptedClaims);
        Assert.Equal(0, result.RejectedClaims);
    }

    [Fact]
    public async Task Explain_RateLimited_ReturnsFailMessage()
    {
        var svc = CreateService(new StubHandler((HttpStatusCode)429, "quota exceeded"), apiKey: "k");

        var result = await svc.ExplainWizardCandidatesAsync("adaylar", "tercihler", null, NoRefs, NoRefs);

        Assert.False(result.Ok);
        Assert.Contains("kota", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }
}
