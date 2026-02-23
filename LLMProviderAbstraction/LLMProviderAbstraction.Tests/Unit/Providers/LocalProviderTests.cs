using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LLMProviderAbstraction.Configuration;
using LLMProviderAbstraction.Models;
using LLMProviderAbstraction.Providers;
using Moq;
using Moq.Protected;
using Xunit;

namespace LLMProviderAbstraction.Tests.Unit.Providers;

/// <summary>
/// Unit tests for the LocalProvider class
/// </summary>
public class LocalProviderTests
{
    private readonly LLMConfiguration _validConfig;

    public LocalProviderTests()
    {
        _validConfig = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "llama2",
            Endpoint = "http://localhost:11434",
            TimeoutSeconds = 30,
            MaxRetries = 3
        };
    }

    #region Helper Methods

    /// <summary>
    /// Creates a mock HttpMessageHandler that returns the specified response
    /// </summary>
    private Mock<HttpMessageHandler> CreateMockHttpMessageHandler(
        HttpStatusCode statusCode,
        string responseContent)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });
        return mockHandler;
    }

    /// <summary>
    /// Creates a mock HttpMessageHandler that throws the specified exception
    /// </summary>
    private Mock<HttpMessageHandler> CreateMockHttpMessageHandlerWithException(Exception exception)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);
        return mockHandler;
    }

    /// <summary>
    /// Creates a successful OpenAI-compatible API response
    /// </summary>
    private string CreateSuccessResponse(string content)
    {
        return JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        role = "assistant",
                        content = content
                    }
                }
            },
            usage = new
            {
                prompt_tokens = 10,
                completion_tokens = 20,
                total_tokens = 30
            }
        });
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    private string CreateErrorResponse(string errorMessage)
    {
        return JsonSerializer.Serialize(new
        {
            error = new
            {
                message = errorMessage
            }
        });
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidConfiguration_CreatesProvider()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var provider = new LocalProvider(_validConfig, httpClient);

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        Action act = () => new LocalProvider(null!, httpClient);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("config");
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new LocalProvider(_validConfig, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_WithMissingEndpoint_ThrowsArgumentException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "test-model"
        };
        var httpClient = new HttpClient();

        // Act
        Action act = () => new LocalProvider(config, httpClient);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Endpoint is required*")
            .WithParameterName("config");
    }

    [Fact]
    public void Constructor_WithMissingModelIdentifier_ThrowsArgumentException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            Endpoint = "http://localhost:11434"
        };
        var httpClient = new HttpClient();

        // Act
        Action act = () => new LocalProvider(config, httpClient);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ModelIdentifier is required*")
            .WithParameterName("config");
    }

    [Fact]
    public void Constructor_WithEmptyEndpoint_ThrowsArgumentException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "test-model",
            Endpoint = "   "
        };
        var httpClient = new HttpClient();

        // Act
        Action act = () => new LocalProvider(config, httpClient);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Endpoint is required*");
    }

    [Fact]
    public void Constructor_SetsTimeoutFromConfiguration()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "test-model",
            Endpoint = "http://localhost:11434",
            TimeoutSeconds = 60
        };
        var httpClient = new HttpClient();

        // Act
        var provider = new LocalProvider(config, httpClient);

        // Assert
        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(60));
    }

    #endregion

    #region AnalyzeAsync Success Tests

    [Fact]
    public async Task AnalyzeAsync_WithSuccessfulResponse_ReturnsSuccess()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandler(
            HttpStatusCode.OK,
            CreateSuccessResponse("This is the response content"));
        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new LocalProvider(_validConfig, httpClient);

        // Act
        var result = await provider.AnalyzeAsync("test context", "test prompt");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Content.Should().Be("This is the response content");
        result.Error.Should().BeNull();
        result.Metadata.Should().ContainKey("ModelId");
        result.Metadata.Should().ContainKey("Endpoint");
        result.Metadata.Should().ContainKey("InputTokens");
        result.Metadata.Should().ContainKey("OutputTokens");
        result.Metadata.Should().ContainKey("TotalTokens");
    }

    [Fact]
    public async Task AnalyzeAsync_WithContextAndPrompt_FormatsCorrectly()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandler(
            HttpStatusCode.OK,
            CreateSuccessResponse("Response"));
        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new LocalProvider(_validConfig, httpClient);

        // Act
        var result = await provider.AnalyzeAsync("context text", "prompt text");

        // Assert
        result.Success.Should().BeTrue();
        
        // Verify the request was made
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.ToString().EndsWith("/v1/chat/completions")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeAsync_WithEmptyContext_UsesOnlyPrompt()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandler(
            HttpStatusCode.OK,
            CreateSuccessResponse("Response"));
        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new LocalProvider(_validConfig, httpClient);

        // Act
        var result = await provider.AnalyzeAsync("", "prompt text");

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region AnalyzeAsync HTTP Error Tests

    [Fact]
    public async Task AnalyzeAsync_WithHttp400_ReturnsValidationError()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandler(
            HttpStatusCode.BadRequest,
            CreateErrorResponse("Invalid request format"));
        var httpClient = new HttpClient(mockHandler.Object);
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "test-model",
            Endpoint = "http://localhost:11434",
            MaxRetries = 0 // No retries for faster test
        };
        var provider = new LocalProvider(config, httpClient);

        // Act
        var result = await provider.AnalyzeAsync("context", "prompt");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(ErrorType.ValidationError);
        result.Error.Message.Should().Contain("Invalid request");
    }

    [Fact]
    public async Task AnalyzeAsync_WithHttp401_ReturnsAuthenticationError()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandler(
            HttpStatusCode.Unauthorized,
            CreateErrorResponse("Unauthorized"));
        var httpClient = new HttpClient(mockHandler.Object);
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "test-model",
            Endpoint = "http://localhost:11434",
            MaxRetries = 0
        };
        var provider = new LocalProvider(config, httpClient);

        // Act
        var result = await provider.AnalyzeAsync("context", "prompt");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(ErrorType.AuthenticationError);
        result.Error.Message.Should().Contain("Authentication failed");
    }

    [Fact]
    public async Task AnalyzeAsync_WithHttp404_ReturnsProviderError()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandler(
            HttpStatusCode.NotFound,
            CreateErrorResponse("Endpoint not found"));
        var httpClient = new HttpClient(mockHandler.Object);
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "test-model",
            Endpoint = "http://localhost:11434",
            MaxRetries = 0
        };
        var provider = new LocalProvider(config, httpClient);

        // Act
        var result = await provider.AnalyzeAsync("context", "prompt");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(ErrorType.ProviderError);
        result.Error.Message.Should().Contain("Endpoint not found");
    }

    [Fact]
    public async Task AnalyzeAsync_WithHttp429_ReturnsRateLimitError()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandler(
            (HttpStatusCode)429,
            CreateErrorResponse("Rate limit exceeded"));
        var httpClient = new HttpClient(mockHandler.Object);
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "test-model",
            Endpoint = "http://localhost:11434",
            MaxRetries = 0
        };
        var provider = new LocalProvider(config, httpClient);

        // Act
        var result = await provider.AnalyzeAsync("context", "prompt");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(ErrorType.RateLimitError);
        result.Error.Message.Should().Contain("Rate limit exceeded");
    }

    [Fact]
    public async Task AnalyzeAsync_WithHttp500_ReturnsProviderError()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandler(
            HttpStatusCode.InternalServerError,
            CreateErrorResponse("Internal server error"));
        var httpClient = new HttpClient(mockHandler.Object);
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "test-model",
            Endpoint = "http://localhost:11434",
            MaxRetries = 0
        };
        var provider = new LocalProvider(config, httpClient);

        // Act
        var result = await provider.AnalyzeAsync("context", "prompt");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(ErrorType.ProviderError);
        result.Error.Message.Should().Contain("server error");
    }

    #endregion

    #region AnalyzeAsync Network Error Tests

    [Fact]
    public async Task AnalyzeAsync_WithNetworkTimeout_ReturnsConnectionError()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandlerWithException(
            new TaskCanceledException("The operation was canceled."));
        var httpClient = new HttpClient(mockHandler.Object)
        {
            Timeout = TimeSpan.FromSeconds(1)
        };
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "test-model",
            Endpoint = "http://localhost:11434",
            TimeoutSeconds = 1,
            MaxRetries = 0
        };
        var provider = new LocalProvider(config, httpClient);

        // Act
        var result = await provider.AnalyzeAsync("context", "prompt");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(ErrorType.ConnectionError);
        result.Error.Message.Should().Contain("timed out");
    }

    [Fact]
    public async Task AnalyzeAsync_WithConnectionRefused_ReturnsConnectionError()
    {
        // Arrange
        var socketException = new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.ConnectionRefused);
        var httpRequestException = new HttpRequestException("Connection refused", socketException);
        var mockHandler = CreateMockHttpMessageHandlerWithException(httpRequestException);
        var httpClient = new HttpClient(mockHandler.Object);
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "test-model",
            Endpoint = "http://localhost:11434",
            MaxRetries = 0
        };
        var provider = new LocalProvider(config, httpClient);

        // Act
        var result = await provider.AnalyzeAsync("context", "prompt");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(ErrorType.ConnectionError);
        result.Error.Message.Should().Contain("Failed to connect");
    }

    #endregion

    #region AnalyzeAsync Retry Logic Tests

    [Fact]
    public async Task AnalyzeAsync_WithTransientError_RetriesWithExponentialBackoff()
    {
        // Arrange
        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 3)
                {
                    // First two calls fail with rate limit
                    return new HttpResponseMessage
                    {
                        StatusCode = (HttpStatusCode)429,
                        Content = new StringContent(CreateErrorResponse("Rate limit"), Encoding.UTF8, "application/json")
                    };
                }
                // Third call succeeds
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(CreateSuccessResponse("Success after retry"), Encoding.UTF8, "application/json")
                };
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "test-model",
            Endpoint = "http://localhost:11434",
            MaxRetries = 3
        };
        var provider = new LocalProvider(config, httpClient);

        // Act
        var result = await provider.AnalyzeAsync("context", "prompt");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Content.Should().Be("Success after retry");
        callCount.Should().Be(3); // Initial attempt + 2 retries
    }

    [Fact]
    public async Task AnalyzeAsync_WithNonTransientError_DoesNotRetry()
    {
        // Arrange
        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(CreateErrorResponse("Bad request"), Encoding.UTF8, "application/json")
                };
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "test-model",
            Endpoint = "http://localhost:11434",
            MaxRetries = 3
        };
        var provider = new LocalProvider(config, httpClient);

        // Act
        var result = await provider.AnalyzeAsync("context", "prompt");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.ValidationError);
        callCount.Should().Be(1); // Only initial attempt, no retries
    }

    #endregion

    #region SendMessageAsync Success Tests

    [Fact]
    public async Task SendMessageAsync_WithSuccessfulResponse_ReturnsSuccess()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandler(
            HttpStatusCode.OK,
            CreateSuccessResponse("Assistant response"));
        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new LocalProvider(_validConfig, httpClient);
        var session = new LLMProviderAbstraction.Session.Session("test-session");

        // Act
        var result = await provider.SendMessageAsync(session, "User message");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Content.Should().Be("Assistant response");
        result.Metadata.Should().ContainKey("SessionId");
        result.Metadata["SessionId"].Should().Be("test-session");
    }

    [Fact]
    public async Task SendMessageAsync_WithSessionHistory_IncludesAllMessages()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandler(
            HttpStatusCode.OK,
            CreateSuccessResponse("Response"));
        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new LocalProvider(_validConfig, httpClient);
        var session = new LLMProviderAbstraction.Session.Session("test-session");
        
        // Add some history
        session.AddMessage(new Message("Previous user message", MessageRole.User));
        session.AddMessage(new Message("Previous assistant message", MessageRole.Assistant));

        // Act
        var result = await provider.SendMessageAsync(session, "New message");

        // Assert
        result.Success.Should().BeTrue();
        
        // Verify the request was made
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendMessageAsync_Success_AddsMessagesToSession()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandler(
            HttpStatusCode.OK,
            CreateSuccessResponse("Assistant response"));
        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new LocalProvider(_validConfig, httpClient);
        var session = new LLMProviderAbstraction.Session.Session("test-session");
        var initialMessageCount = session.Messages.Count;

        // Act
        var result = await provider.SendMessageAsync(session, "User message");

        // Assert
        result.Success.Should().BeTrue();
        session.Messages.Count.Should().Be(initialMessageCount + 2); // User + Assistant
        session.Messages[^2].Content.Should().Be("User message");
        session.Messages[^2].Role.Should().Be(MessageRole.User);
        session.Messages[^1].Content.Should().Be("Assistant response");
        session.Messages[^1].Role.Should().Be(MessageRole.Assistant);
    }

    [Fact]
    public async Task SendMessageAsync_WithNullSession_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new LocalProvider(_validConfig, httpClient);

        // Act
        Func<Task> act = async () => await provider.SendMessageAsync(null!, "message");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("session");
    }

    [Fact]
    public async Task SendMessageAsync_WithEmptyMessage_ThrowsArgumentException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new LocalProvider(_validConfig, httpClient);
        var session = new LLMProviderAbstraction.Session.Session("test-session");

        // Act
        Func<Task> act = async () => await provider.SendMessageAsync(session, "");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Message cannot be null or empty*")
            .WithParameterName("message");
    }

    [Fact]
    public async Task SendMessageAsync_WithWhitespaceMessage_ThrowsArgumentException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new LocalProvider(_validConfig, httpClient);
        var session = new LLMProviderAbstraction.Session.Session("test-session");

        // Act
        Func<Task> act = async () => await provider.SendMessageAsync(session, "   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Message cannot be null or empty*")
            .WithParameterName("message");
    }

    #endregion

    #region SendMessageAsync Error Tests

    [Fact]
    public async Task SendMessageAsync_WithError_DoesNotAddMessagesToSession()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandler(
            HttpStatusCode.InternalServerError,
            CreateErrorResponse("Server error"));
        var httpClient = new HttpClient(mockHandler.Object);
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "test-model",
            Endpoint = "http://localhost:11434",
            MaxRetries = 0
        };
        var provider = new LocalProvider(config, httpClient);
        var session = new LLMProviderAbstraction.Session.Session("test-session");
        var initialMessageCount = session.Messages.Count;

        // Act
        var result = await provider.SendMessageAsync(session, "User message");

        // Assert
        result.Success.Should().BeFalse();
        session.Messages.Count.Should().Be(initialMessageCount); // No messages added
    }

    #endregion

    #region ValidateAsync Tests

    [Fact]
    public async Task ValidateAsync_WithSuccessfulConnection_ReturnsSuccess()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(new { data = new[] { new { id = "model1" } } }));
        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new LocalProvider(_validConfig, httpClient);

        // Act
        var result = await provider.ValidateAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithHttp404_ReturnsSuccess()
    {
        // Arrange - 404 is acceptable as it means server is reachable
        var mockHandler = CreateMockHttpMessageHandler(
            HttpStatusCode.NotFound,
            "Not found");
        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new LocalProvider(_validConfig, httpClient);

        // Act
        var result = await provider.ValidateAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithHttp401_ReturnsFailure()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandler(
            HttpStatusCode.Unauthorized,
            "Unauthorized");
        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new LocalProvider(_validConfig, httpClient);

        // Act
        var result = await provider.ValidateAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.First().Should().Contain("Authentication failed");
    }

    [Fact]
    public async Task ValidateAsync_WithConnectionRefused_ReturnsFailure()
    {
        // Arrange
        var socketException = new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.ConnectionRefused);
        var httpRequestException = new HttpRequestException("Connection refused", socketException);
        var mockHandler = CreateMockHttpMessageHandlerWithException(httpRequestException);
        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new LocalProvider(_validConfig, httpClient);

        // Act
        var result = await provider.ValidateAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.First().Should().Contain("Connection refused");
    }

    [Fact]
    public async Task ValidateAsync_WithTimeout_ReturnsFailure()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandlerWithException(
            new TaskCanceledException("The operation was canceled."));
        var httpClient = new HttpClient(mockHandler.Object)
        {
            Timeout = TimeSpan.FromSeconds(1)
        };
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            ModelIdentifier = "test-model",
            Endpoint = "http://localhost:11434",
            TimeoutSeconds = 1
        };
        var provider = new LocalProvider(config, httpClient);

        // Act
        var result = await provider.ValidateAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.First().Should().Contain("timeout");
    }

    #endregion
}
