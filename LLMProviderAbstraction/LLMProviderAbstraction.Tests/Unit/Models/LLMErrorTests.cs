using FluentAssertions;
using LLMProviderAbstraction.Models;
using Xunit;

namespace LLMProviderAbstraction.Tests.Unit.Models;

/// <summary>
/// Unit tests for the LLMError class
/// </summary>
public class LLMErrorTests
{
    [Fact]
    public void Constructor_WithTypeAndMessage_CreatesErrorWithoutInnerException()
    {
        // Arrange
        var type = ErrorType.ConnectionError;
        var message = "Connection failed";

        // Act
        var error = new LLMError(type, message);

        // Assert
        error.Type.Should().Be(type);
        error.Message.Should().Be(message);
        error.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithInnerException_CreatesErrorWithInnerException()
    {
        // Arrange
        var type = ErrorType.ConnectionError;
        var message = "Connection failed";
        var innerException = new InvalidOperationException("Network timeout");

        // Act
        var error = new LLMError(type, message, innerException);

        // Assert
        error.Type.Should().Be(type);
        error.Message.Should().Be(message);
        error.InnerException.Should().Be(innerException);
        error.InnerException.Message.Should().Be("Network timeout");
    }

    [Fact]
    public void Constructor_WithValidationError_CreatesValidationError()
    {
        // Arrange
        var message = "Invalid configuration";

        // Act
        var error = new LLMError(ErrorType.ValidationError, message);

        // Assert
        error.Type.Should().Be(ErrorType.ValidationError);
        error.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithAuthenticationError_CreatesAuthenticationError()
    {
        // Arrange
        var message = "Authentication failed";

        // Act
        var error = new LLMError(ErrorType.AuthenticationError, message);

        // Assert
        error.Type.Should().Be(ErrorType.AuthenticationError);
        error.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithRateLimitError_CreatesRateLimitError()
    {
        // Arrange
        var message = "Rate limit exceeded";

        // Act
        var error = new LLMError(ErrorType.RateLimitError, message);

        // Assert
        error.Type.Should().Be(ErrorType.RateLimitError);
        error.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithProviderError_CreatesProviderError()
    {
        // Arrange
        var message = "Provider error occurred";

        // Act
        var error = new LLMError(ErrorType.ProviderError, message);

        // Assert
        error.Type.Should().Be(ErrorType.ProviderError);
        error.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithUnknownError_CreatesUnknownError()
    {
        // Arrange
        var message = "Unknown error occurred";

        // Act
        var error = new LLMError(ErrorType.UnknownError, message);

        // Assert
        error.Type.Should().Be(ErrorType.UnknownError);
        error.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithEmptyMessage_CreatesErrorWithEmptyMessage()
    {
        // Arrange
        var type = ErrorType.ConnectionError;
        var message = string.Empty;

        // Act
        var error = new LLMError(type, message);

        // Assert
        error.Type.Should().Be(type);
        error.Message.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullInnerException_CreatesErrorWithoutInnerException()
    {
        // Arrange
        var type = ErrorType.ConnectionError;
        var message = "Connection failed";

        // Act
        var error = new LLMError(type, message, null);

        // Assert
        error.Type.Should().Be(type);
        error.Message.Should().Be(message);
        error.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNestedInnerException_PreservesExceptionChain()
    {
        // Arrange
        var type = ErrorType.ConnectionError;
        var message = "Connection failed";
        var rootException = new ArgumentException("Invalid argument");
        var innerException = new InvalidOperationException("Network timeout", rootException);

        // Act
        var error = new LLMError(type, message, innerException);

        // Assert
        error.InnerException.Should().Be(innerException);
        error.InnerException!.InnerException.Should().Be(rootException);
    }

    [Fact]
    public void Constructor_WithVeryLongMessage_PreservesMessage()
    {
        // Arrange
        var type = ErrorType.ProviderError;
        var message = new string('x', 10000);

        // Act
        var error = new LLMError(type, message);

        // Assert
        error.Message.Should().Be(message);
        error.Message.Length.Should().Be(10000);
    }

    [Fact]
    public void Constructor_WithSpecialCharactersInMessage_PreservesMessage()
    {
        // Arrange
        var type = ErrorType.ValidationError;
        var message = "Error: !@#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act
        var error = new LLMError(type, message);

        // Assert
        error.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithUnicodeCharactersInMessage_PreservesMessage()
    {
        // Arrange
        var type = ErrorType.ProviderError;
        var message = "Error: 你好 مرحبا שלום";

        // Act
        var error = new LLMError(type, message);

        // Assert
        error.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMultilineMessage_PreservesMessage()
    {
        // Arrange
        var type = ErrorType.ProviderError;
        var message = "Line 1\nLine 2\nLine 3";

        // Act
        var error = new LLMError(type, message);

        // Assert
        error.Message.Should().Be(message);
    }

    [Theory]
    [InlineData(ErrorType.ValidationError)]
    [InlineData(ErrorType.ConnectionError)]
    [InlineData(ErrorType.AuthenticationError)]
    [InlineData(ErrorType.RateLimitError)]
    [InlineData(ErrorType.ProviderError)]
    [InlineData(ErrorType.UnknownError)]
    public void Constructor_WithAllErrorTypes_CreatesCorrectErrorType(ErrorType errorType)
    {
        // Arrange
        var message = $"Error of type {errorType}";

        // Act
        var error = new LLMError(errorType, message);

        // Assert
        error.Type.Should().Be(errorType);
        error.Message.Should().Be(message);
    }
}
