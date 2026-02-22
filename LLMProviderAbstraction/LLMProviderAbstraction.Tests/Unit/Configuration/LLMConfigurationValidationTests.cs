using FluentAssertions;
using LLMProviderAbstraction.Configuration;
using LLMProviderAbstraction.Models;
using Xunit;

namespace LLMProviderAbstraction.Tests.Unit.Configuration;

/// <summary>
/// Unit tests for LLMConfiguration validation logic
/// </summary>
public class LLMConfigurationValidationTests
{
    #region Valid Configuration Tests

    [Fact]
    public void Validate_WithValidBedrockConfiguration_ReturnsSuccess()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Bedrock,
            ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0",
            AccessKey = "AKIAIOSFODNN7EXAMPLE",
            SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
            Region = "us-east-1",
            TimeoutSeconds = 30,
            MaxRetries = 3
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithValidBedrockConfigurationWithoutRegion_ReturnsSuccess()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Bedrock,
            ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0",
            AccessKey = "AKIAIOSFODNN7EXAMPLE",
            SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithValidLocalConfiguration_ReturnsSuccess()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = "http://localhost:11434",
            TimeoutSeconds = 60,
            MaxRetries = 5
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithValidLocalConfigurationHttps_ReturnsSuccess()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "mistral",
            Endpoint = "https://api.example.com/v1"
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    #endregion

    #region Missing Required Fields Tests

    [Fact]
    public void Validate_WithMissingModelIdentifier_ReturnsFailure()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Bedrock,
            AccessKey = "AKIAIOSFODNN7EXAMPLE",
            SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain(e => e.Contains("ModelIdentifier"));
    }

    [Fact]
    public void Validate_WithEmptyModelIdentifier_ReturnsFailure()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Bedrock,
            ModelIdentifier = "",
            AccessKey = "AKIAIOSFODNN7EXAMPLE",
            SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("ModelIdentifier"));
    }

    [Fact]
    public void Validate_WithWhitespaceModelIdentifier_ReturnsFailure()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Bedrock,
            ModelIdentifier = "   ",
            AccessKey = "AKIAIOSFODNN7EXAMPLE",
            SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("ModelIdentifier"));
    }

    [Fact]
    public void Validate_BedrockWithMissingAccessKey_ReturnsFailure()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Bedrock,
            ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0",
            SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("AccessKey"));
    }

    [Fact]
    public void Validate_BedrockWithEmptyAccessKey_ReturnsFailure()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Bedrock,
            ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0",
            AccessKey = "",
            SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("AccessKey"));
    }

    [Fact]
    public void Validate_BedrockWithMissingSecretKey_ReturnsFailure()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Bedrock,
            ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0",
            AccessKey = "AKIAIOSFODNN7EXAMPLE"
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("SecretKey"));
    }

    [Fact]
    public void Validate_BedrockWithEmptySecretKey_ReturnsFailure()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Bedrock,
            ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0",
            AccessKey = "AKIAIOSFODNN7EXAMPLE",
            SecretKey = ""
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("SecretKey"));
    }

    [Fact]
    public void Validate_BedrockWithMissingBothKeys_ReturnsMultipleErrors()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Bedrock,
            ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0"
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.Contains("AccessKey"));
        result.Errors.Should().Contain(e => e.Contains("SecretKey"));
    }

    [Fact]
    public void Validate_LocalWithMissingEndpoint_ReturnsFailure()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2"
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Endpoint"));
    }

    [Fact]
    public void Validate_LocalWithEmptyEndpoint_ReturnsFailure()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = ""
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Endpoint"));
    }

    #endregion

    #region Invalid Endpoint URI Format Tests

    [Fact]
    public void Validate_LocalWithInvalidEndpointFormat_ReturnsFailure()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = "not-a-valid-uri"
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("URI"));
    }

    [Fact]
    public void Validate_LocalWithRelativeUri_ReturnsFailure()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = "/api/v1/chat"
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("URI"));
    }

    [Fact]
    public void Validate_LocalWithMalformedUri_ReturnsFailure()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = "http://[invalid"
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("URI"));
    }

    #endregion

    #region Invalid Timeout and Retry Values Tests

    [Fact]
    public void Validate_WithZeroTimeoutSeconds_ReturnsFailure()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = "http://localhost:11434",
            TimeoutSeconds = 0
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("TimeoutSeconds"));
    }

    [Fact]
    public void Validate_WithNegativeTimeoutSeconds_ReturnsFailure()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = "http://localhost:11434",
            TimeoutSeconds = -10
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("TimeoutSeconds"));
    }

    [Fact]
    public void Validate_WithNegativeMaxRetries_ReturnsFailure()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = "http://localhost:11434",
            MaxRetries = -1
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxRetries"));
    }

    [Fact]
    public void Validate_WithZeroMaxRetries_ReturnsSuccess()
    {
        // Arrange - Zero retries is valid (no retry attempts)
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = "http://localhost:11434",
            MaxRetries = 0
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    #endregion

    #region Multiple Validation Errors Tests

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Bedrock,
            ModelIdentifier = "",
            TimeoutSeconds = -5,
            MaxRetries = -2
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(1);
        result.Errors.Should().Contain(e => e.Contains("ModelIdentifier"));
        result.Errors.Should().Contain(e => e.Contains("AccessKey"));
        result.Errors.Should().Contain(e => e.Contains("SecretKey"));
        result.Errors.Should().Contain(e => e.Contains("TimeoutSeconds"));
        result.Errors.Should().Contain(e => e.Contains("MaxRetries"));
    }

    [Fact]
    public void Validate_LocalWithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "   ",
            Endpoint = "invalid-uri",
            TimeoutSeconds = 0,
            MaxRetries = -3
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(4);
        result.Errors.Should().Contain(e => e.Contains("ModelIdentifier"));
        result.Errors.Should().Contain(e => e.Contains("URI"));
        result.Errors.Should().Contain(e => e.Contains("TimeoutSeconds"));
        result.Errors.Should().Contain(e => e.Contains("MaxRetries"));
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void Validate_WithVeryLargeTimeoutSeconds_ReturnsSuccess()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = "http://localhost:11434",
            TimeoutSeconds = 3600 // 1 hour
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithVeryLargeMaxRetries_ReturnsSuccess()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = "http://localhost:11434",
            MaxRetries = 100
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEndpointContainingPort_ReturnsSuccess()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = "http://localhost:8080"
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEndpointContainingPath_ReturnsSuccess()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = "http://localhost:11434/api/v1/chat/completions"
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEndpointContainingQueryString_ReturnsSuccess()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = "http://localhost:11434/api?version=1"
        };

        // Act
        var result = config.Validate();

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion
}
