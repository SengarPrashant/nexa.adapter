using LLMProviderAbstraction.Interfaces;

namespace LLMProviderAbstraction.Tests;

/// <summary>
/// Test implementation of IHttpClientFactory for unit and property tests
/// </summary>
public class TestHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient()
    {
        return new HttpClient();
    }
}
