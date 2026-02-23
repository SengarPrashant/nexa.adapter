using Hedgehog;
using Hedgehog.Linq;
using LLMProviderAbstraction.Configuration;
using LLMProviderAbstraction.Models;
using LLMProviderAbstraction.Providers;
using LLMProviderAbstraction.Tests.Generators;
using Xunit;

namespace LLMProviderAbstraction.Tests.Properties;

public class ConfigurationProperties
{
    /// <summary>
    /// Property 1: Provider Creation from Valid Configuration
    /// Validates: Requirements 1.1, 1.2, 4.1, 4.2, 4.3, 4.4
    /// </summary>
    [Fact]
    public void ValidConfigurationCreatesProvider()
    {
        // Run 100 iterations of the property test
        var gen = ConfigurationGenerators.ValidConfiguration();
        var samples = gen.Sample(size: 10, count: 100);
        
        foreach (var config in samples)
        {
            var factory = new LLMProviderFactory(new TestHttpClientFactory());
            var provider = factory.CreateProvider(config);
            
            var isCorrectType = config.ProviderType switch
            {
                ProviderType.Bedrock => provider is BedrockProvider,
                ProviderType.Local => provider is LocalProvider,
                _ => false
            };
            
            Assert.True(provider != null && isCorrectType, 
                $"Provider creation failed for {config.ProviderType}");
        }
    }

    /// <summary>
    /// Property 10: Invalid Configuration Returns Validation Error
    /// Validates: Requirements 5.1
    /// </summary>
    [Fact]
    public void InvalidConfigurationReturnsValidationError()
    {
        // Run 100 iterations of the property test
        var gen = ConfigurationGenerators.InvalidConfiguration();
        var samples = gen.Sample(size: 10, count: 100);
        
        foreach (var config in samples)
        {
            var result = config.Validate();
            Assert.False(result.Success, 
                $"Invalid configuration should fail validation. Config: ProviderType={config.ProviderType}, " +
                $"ModelId={config.ModelIdentifier}, AccessKey={config.AccessKey}, SecretKey={config.SecretKey}, " +
                $"Endpoint={config.Endpoint}, Timeout={config.TimeoutSeconds}, Retries={config.MaxRetries}");
            Assert.True(result.Errors.Count > 0, "Invalid configuration should have error messages");
            
            // Verify error messages are descriptive (non-empty and contain meaningful text)
            foreach (var error in result.Errors)
            {
                Assert.False(string.IsNullOrWhiteSpace(error), "Error messages should be descriptive and non-empty");
                Assert.True(error.Length > 10, $"Error message should be descriptive, but was: '{error}'");
            }
        }
    }
}
