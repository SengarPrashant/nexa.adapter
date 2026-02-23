using FluentAssertions;
using LLMProviderAbstraction.Models;
using Xunit;

namespace LLMProviderAbstraction.Tests.Unit.Models;

/// <summary>
/// Unit tests for the LLMResponse class
/// </summary>
public class LLMResponseTests
{
    [Fact]
    public void CreateSuccess_WithContent_CreatesSuccessfulResponse()
    {
        // Arrange
        var content = "This is a successful response";

        // Act
        var response = LLMResponse.CreateSuccess(content);

        // Assert
        response.Success.Should().BeTrue();
        response.Content.Should().Be(content);
        response.Error.Should().BeNull();
        response.Metadata.Should().NotBeNull();
        response.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void CreateSuccess_WithContentAndMetadata_CreatesResponseWithMetadata()
    {
        // Arrange
        var content = "Response with metadata";
        var metadata = new Dictionary<string, object>
        {
            { "model", "gpt-4" },
            { "tokens", 150 },
            { "temperature", 0.7 }
        };

        // Act
        var response = LLMResponse.CreateSuccess(content, metadata);

        // Assert
        response.Success.Should().BeTrue();
        response.Content.Should().Be(content);
        response.Error.Should().BeNull();
        response.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public void CreateSuccess_WithNullMetadata_CreatesEmptyMetadata()
    {
        // Arrange
        var content = "Response without metadata";

        // Act
        var response = LLMResponse.CreateSuccess(content, null);

        // Assert
        response.Success.Should().BeTrue();
        response.Metadata.Should().NotBeNull();
        response.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void CreateSuccess_WithEmptyString_CreatesSuccessfulResponse()
    {
        // Arrange
        var content = string.Empty;

        // Act
        var response = LLMResponse.CreateSuccess(content);

        // Assert
        response.Success.Should().BeTrue();
        response.Content.Should().BeEmpty();
        response.Error.Should().BeNull();
    }

    [Fact]
    public void CreateError_WithError_CreatesFailedResponse()
    {
        // Arrange
        var error = new LLMError(ErrorType.ConnectionError, "Connection failed");

        // Act
        var response = LLMResponse.CreateError(error);

        // Assert
        response.Success.Should().BeFalse();
        response.Content.Should().BeNull();
        response.Error.Should().Be(error);
        response.Metadata.Should().NotBeNull();
        response.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void CreateError_WithValidationError_CreatesFailedResponse()
    {
        // Arrange
        var error = new LLMError(ErrorType.ValidationError, "Invalid configuration");

        // Act
        var response = LLMResponse.CreateError(error);

        // Assert
        response.Success.Should().BeFalse();
        response.Error.Should().Be(error);
        response.Error!.Type.Should().Be(ErrorType.ValidationError);
    }

    [Fact]
    public void CreateError_WithAuthenticationError_CreatesFailedResponse()
    {
        // Arrange
        var error = new LLMError(ErrorType.AuthenticationError, "Authentication failed");

        // Act
        var response = LLMResponse.CreateError(error);

        // Assert
        response.Success.Should().BeFalse();
        response.Error!.Type.Should().Be(ErrorType.AuthenticationError);
    }

    [Fact]
    public void CreateError_WithRateLimitError_CreatesFailedResponse()
    {
        // Arrange
        var error = new LLMError(ErrorType.RateLimitError, "Rate limit exceeded");

        // Act
        var response = LLMResponse.CreateError(error);

        // Assert
        response.Success.Should().BeFalse();
        response.Error!.Type.Should().Be(ErrorType.RateLimitError);
    }

    [Fact]
    public void CreateError_WithProviderError_CreatesFailedResponse()
    {
        // Arrange
        var error = new LLMError(ErrorType.ProviderError, "Provider error occurred");

        // Act
        var response = LLMResponse.CreateError(error);

        // Assert
        response.Success.Should().BeFalse();
        response.Error!.Type.Should().Be(ErrorType.ProviderError);
    }

    [Fact]
    public void CreateError_WithUnknownError_CreatesFailedResponse()
    {
        // Arrange
        var error = new LLMError(ErrorType.UnknownError, "Unknown error occurred");

        // Act
        var response = LLMResponse.CreateError(error);

        // Assert
        response.Success.Should().BeFalse();
        response.Error!.Type.Should().Be(ErrorType.UnknownError);
    }

    [Fact]
    public void CreateSuccess_WithVeryLongContent_PreservesContent()
    {
        // Arrange
        var content = new string('x', 50000);

        // Act
        var response = LLMResponse.CreateSuccess(content);

        // Assert
        response.Success.Should().BeTrue();
        response.Content.Should().Be(content);
        response.Content!.Length.Should().Be(50000);
    }

    [Fact]
    public void CreateSuccess_WithSpecialCharacters_PreservesContent()
    {
        // Arrange
        var content = "Special: !@#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act
        var response = LLMResponse.CreateSuccess(content);

        // Assert
        response.Content.Should().Be(content);
    }

    [Fact]
    public void Metadata_CanBeModifiedAfterCreation()
    {
        // Arrange
        var response = LLMResponse.CreateSuccess("test");

        // Act
        response.Metadata["key"] = "value";

        // Assert
        response.Metadata.Should().ContainKey("key");
        response.Metadata["key"].Should().Be("value");
    }
}
