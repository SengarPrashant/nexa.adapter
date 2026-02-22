using FluentAssertions;
using LLMProviderAbstraction.Configuration;
using LLMProviderAbstraction.Interfaces;
using LLMProviderAbstraction.Models;
using LLMProviderAbstraction.Providers;
using LLMProviderAbstraction.Tests;
using Xunit;

namespace LLMProviderAbstraction.Tests.Unit.Factory;

/// <summary>
/// Unit tests for the LLMProviderFactory class
/// </summary>
public class LLMProviderFactoryTests
{
    private readonly IHttpClientFactory _httpClientFactory;

    public LLMProviderFactoryTests()
    {
        _httpClientFactory = new TestHttpClientFactory();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidHttpClientFactory_CreatesFactory()
    {
        // Act
        var factory = new LLMProviderFactory(_httpClientFactory);

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullHttpClientFactory_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new LLMProviderFactory(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClientFactory");
    }

    #endregion

    #region CreateProvider Tests - Bedrock

    [Fact]
    public void CreateProvider_WithBedrockConfiguration_ReturnsBedrockProvider()
    {
        // Arrange
        var factory = new LLMProviderFactory(_httpClientFactory);
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Bedrock,
            ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0",
            AccessKey = "test-access-key",
            SecretKey = "test-secret-key",
            Region = "us-east-1"
        };

        // Act
        var provider = factory.CreateProvider(config);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<BedrockProvider>();
    }

    [Fact]
    public void CreateProvider_WithBedrockConfiguration_ReturnsILLMProvider()
    {
        // Arrange
        var factory = new LLMProviderFactory(_httpClientFactory);
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Bedrock,
            ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0",
            AccessKey = "test-access-key",
            SecretKey = "test-secret-key",
            Region = "us-west-2"
        };

        // Act
        var provider = factory.CreateProvider(config);

        // Assert
        provider.Should().BeAssignableTo<ILLMProvider>();
    }

    #endregion

    #region CreateProvider Tests - Local

    [Fact]
    public void CreateProvider_WithLocalConfiguration_ReturnsLocalProvider()
    {
        // Arrange
        var factory = new LLMProviderFactory(_httpClientFactory);
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = "http://localhost:11434"
        };

        // Act
        var provider = factory.CreateProvider(config);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<LocalProvider>();
    }

    [Fact]
    public void CreateProvider_WithLocalConfiguration_ReturnsILLMProvider()
    {
        // Arrange
        var factory = new LLMProviderFactory(_httpClientFactory);
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "mistral",
            Endpoint = "http://localhost:8080"
        };

        // Act
        var provider = factory.CreateProvider(config);

        // Assert
        provider.Should().BeAssignableTo<ILLMProvider>();
    }

    #endregion

    #region CreateProvider Tests - Error Cases

    [Fact]
    public void CreateProvider_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var factory = new LLMProviderFactory(_httpClientFactory);

        // Act
        Action act = () => factory.CreateProvider(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("config");
    }

    [Fact]
    public void CreateProvider_WithInvalidProviderType_ThrowsArgumentException()
    {
        // Arrange
        var factory = new LLMProviderFactory(_httpClientFactory);
        var config = new LLMConfiguration
        {
            ProviderType = (ProviderType)999, // Invalid enum value
            ModelIdentifier = "test-model"
        };

        // Act
        Action act = () => factory.CreateProvider(config);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Unsupported provider type: 999*")
            .WithParameterName("config");
    }

    #endregion

    #region CreateProvider Tests - Multiple Instances

    [Fact]
    public void CreateProvider_CalledMultipleTimes_ReturnsNewInstances()
    {
        // Arrange
        var factory = new LLMProviderFactory(_httpClientFactory);
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = "http://localhost:11434"
        };

        // Act
        var provider1 = factory.CreateProvider(config);
        var provider2 = factory.CreateProvider(config);

        // Assert
        provider1.Should().NotBeSameAs(provider2);
    }

    [Fact]
    public void CreateProvider_WithDifferentConfigurations_ReturnsCorrectProviderTypes()
    {
        // Arrange
        var factory = new LLMProviderFactory(_httpClientFactory);
        var bedrockConfig = new LLMConfiguration
        {
            ProviderType = ProviderType.Bedrock,
            ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0",
            AccessKey = "test-access-key",
            SecretKey = "test-secret-key",
            Region = "us-east-1"
        };
        var localConfig = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = "http://localhost:11434"
        };

        // Act
        var bedrockProvider = factory.CreateProvider(bedrockConfig);
        var localProvider = factory.CreateProvider(localConfig);

        // Assert
        bedrockProvider.Should().BeOfType<BedrockProvider>();
        localProvider.Should().BeOfType<LocalProvider>();
    }

    #endregion
}
