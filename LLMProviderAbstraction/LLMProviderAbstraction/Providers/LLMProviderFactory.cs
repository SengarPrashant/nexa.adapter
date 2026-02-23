using LLMProviderAbstraction.Configuration;
using LLMProviderAbstraction.Interfaces;
using LLMProviderAbstraction.Models;

namespace LLMProviderAbstraction.Providers;

/// <summary>
/// Factory for creating ILLMProvider instances based on configuration
/// </summary>
public class LLMProviderFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Creates a new LLMProviderFactory instance
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HttpClient instances</param>
    /// <exception cref="ArgumentNullException">Thrown when httpClientFactory is null</exception>
    public LLMProviderFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    /// <summary>
    /// Creates an ILLMProvider instance based on the provided configuration
    /// </summary>
    /// <param name="config">Configuration specifying the provider type and settings</param>
    /// <returns>An ILLMProvider instance of the appropriate type</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
    /// <exception cref="ArgumentException">Thrown when provider type is unsupported</exception>
    public ILLMProvider CreateProvider(LLMConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        return config.ProviderType switch
        {
            ProviderType.Bedrock => new BedrockProvider(config),
            ProviderType.Local => new LocalProvider(config, _httpClientFactory.CreateClient()),
            _ => throw new ArgumentException($"Unsupported provider type: {config.ProviderType}", nameof(config))
        };
    }
}
