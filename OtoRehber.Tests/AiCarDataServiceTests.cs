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

    [Fact]
    public async Task GetCarRecommendation_NoApiKey_ReturnsConfigError()
    {
        var svc = CreateService(new StubHandler(HttpStatusCode.OK, "{}"), apiKey: "");

        var result = await svc.GetCarRecommendationAsync("bütçem 500 bin", "Golf, Corolla");

        Assert.Contains("API anahtarı bulunamadı", result);
    }

    [Fact]
    public async Task GetCarRecommendation_SuccessResponse_ReturnsModelText()
    {
        const string geminiJson = """
        {
          "candidates": [
            { "content": { "parts": [ { "text": "Size **Toyota Corolla** öneririm." } ] } }
          ]
        }
        """;
        var handler = new StubHandler(HttpStatusCode.OK, geminiJson);
        var svc = CreateService(handler, apiKey: "test-key-123");

        var result = await svc.GetCarRecommendationAsync("dayanıklı bir araç", "Golf, Corolla");

        Assert.Equal("Size **Toyota Corolla** öneririm.", result);
        Assert.NotNull(handler.LastRequest);
        Assert.Contains("test-key-123", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetCarRecommendation_RateLimited_ReturnsFriendlyMessage()
    {
        var svc = CreateService(new StubHandler((HttpStatusCode)429, "quota exceeded"), apiKey: "test-key-123");

        var result = await svc.GetCarRecommendationAsync("x", "y");

        Assert.Contains("kota", result, StringComparison.OrdinalIgnoreCase);
    }
}
