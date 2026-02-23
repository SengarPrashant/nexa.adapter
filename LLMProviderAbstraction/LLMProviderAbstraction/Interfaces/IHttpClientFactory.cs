namespace LLMProviderAbstraction.Interfaces;

/// <summary>
/// Factory interface for creating HttpClient instances
/// </summary>
public interface IHttpClientFactory
{
    /// <summary>
    /// Creates a new HttpClient instance
    /// </summary>
    /// <returns>A new HttpClient instance</returns>
    HttpClient CreateClient();
}
