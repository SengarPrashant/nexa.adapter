using Hedgehog;
using Hedgehog.Linq;
using LLMProviderAbstraction.Configuration;
using LLMProviderAbstraction.Models;
using LLMProviderAbstraction.Providers;
using LLMProviderAbstraction.Session;
using LLMProviderAbstraction.Tests.Generators;
using Moq;
using Moq.Protected;
using Xunit;

namespace LLMProviderAbstraction.Tests.Properties;

public class ErrorHandlingProperties
{
    /// <summary>
    /// Property 3: Authentication and Connection Failures Return Descriptive Errors
    /// **Validates: Requirements 1.5**
    /// Tests that authentication failures return descriptive error messages
    /// </summary>
    [Fact]
    public async Task AuthenticationAndConnectionFailuresReturnDescriptiveErrors()
    {
        // Generate invalid Bedrock configurations with various credential issues
        var invalidBedrockGen = ConfigurationGenerators.ValidBedrockConfiguration()
            .Select(config => new LLMConfiguration
            {
                ProviderType = ProviderType.Bedrock,
                ModelIdentifier = config.ModelIdentifier,
                AccessKey = "INVALID_ACCESS_KEY_" + Guid.NewGuid().ToString("N").Substring(0, 16),
                SecretKey = "INVALID_SECRET_KEY_" + Guid.NewGuid().ToString("N"),
                Region = config.Region,
                MaxRetries = 0 // No retries for faster test execution
            });

        var invalidBedrockConfigs = invalidBedrockGen.Sample(size: 10, count: 50);

        // Test Bedrock authentication failures
        foreach (var config in invalidBedrockConfigs)
        {
            var provider = new BedrockProvider(config);
            
            // Test ValidateAsync - should return descriptive authentication error
            var validationResult = await provider.ValidateAsync();
            
            Assert.False(validationResult.Success, 
                "Validation should fail with invalid credentials");
            Assert.NotEmpty(validationResult.Errors);
            
            var errorMessage = string.Join(" ", validationResult.Errors);
            Assert.True(
                errorMessage.Contains("Authentication", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("Connection", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("credentials", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("Access", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("denied", StringComparison.OrdinalIgnoreCase),
                $"Error message should be descriptive about authentication/connection failure. Got: {errorMessage}");
        }

        // Generate invalid Local configurations with unreachable endpoints
        var invalidLocalGen = Gen.Int32(Hedgehog.Linq.Range.Constant(10000, 65000))
            .Select(port => new LLMConfiguration
            {
                ProviderType = ProviderType.Local,
                ModelIdentifier = "test-model",
                Endpoint = $"http://localhost:{port}/v1/chat/completions", // Unreachable endpoint
                TimeoutSeconds = 1, // Short timeout for faster test
                MaxRetries = 0 // No retries for faster test execution
            });

        var invalidLocalConfigs = invalidLocalGen.Sample(size: 10, count: 50);

        // Test Local provider connection failures
        foreach (var config in invalidLocalConfigs)
        {
            var provider = new LocalProvider(config, new TestHttpClientFactory().CreateClient());
            
            // Test ValidateAsync - should return descriptive connection error
            var validationResult = await provider.ValidateAsync();
            
            Assert.False(validationResult.Success, 
                "Validation should fail with unreachable endpoint");
            Assert.NotEmpty(validationResult.Errors);
            
            var errorMessage = string.Join(" ", validationResult.Errors);
            Assert.True(
                errorMessage.Contains("Connection", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("connect", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("endpoint", StringComparison.OrdinalIgnoreCase),
                $"Error message should be descriptive about connection failure. Got: {errorMessage}");
        }
    }

    /// <summary>
    /// Property 4: Context Analysis Request-Response Round Trip
    /// **Validates: Requirements 2.1, 2.2**
    /// Tests that AnalyzeAsync accepts context and prompt parameters and returns response with content on success
    /// </summary>
    [Fact]
    public async Task ContextAnalysisRequestResponseRoundTrip()
    {
        // Generate random context and prompt strings (100 iterations)
        var contextGen = MessageGenerators.MessageContent();
        var promptGen = MessageGenerators.MessageContent();
        
        var contexts = contextGen.Sample(size: 10, count: 100);
        var prompts = promptGen.Sample(size: 10, count: 100);
        
        for (int i = 0; i < 100; i++)
        {
            var context = contexts[i];
            var prompt = prompts[i];
            
            // Verify that context and prompt are non-null strings
            Assert.NotNull(context);
            Assert.NotNull(prompt);
            
            // Create a mock HTTP handler that returns a successful response
            var mockHandler = CreateMockHttpMessageHandler(
                System.Net.HttpStatusCode.OK,
                CreateSuccessResponse($"Response to: {prompt}"));
            
            var httpClient = new HttpClient(mockHandler.Object);
            
            // Create provider with mock HTTP client
            var config = new LLMConfiguration
            {
                ProviderType = ProviderType.Local,
                ModelIdentifier = "test-model",
                Endpoint = "http://localhost:11434",
                TimeoutSeconds = 30,
                MaxRetries = 0
            };
            
            var provider = new LocalProvider(config, httpClient);
            
            // Call AnalyzeAsync with context and prompt
            var response = await provider.AnalyzeAsync(context, prompt);
            
            // Verify response is successful and contains content
            Assert.True(response.Success, "Response should be successful");
            Assert.NotNull(response.Content);
            Assert.False(string.IsNullOrEmpty(response.Content), 
                "Response content should be non-empty on success");
        }
    }
    
    /// <summary>
    /// Creates a mock HttpMessageHandler that returns the specified response
    /// </summary>
    private Moq.Mock<HttpMessageHandler> CreateMockHttpMessageHandler(
        System.Net.HttpStatusCode statusCode,
        string responseContent)
    {
        var mockHandler = new Moq.Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                Moq.Protected.ItExpr.IsAny<HttpRequestMessage>(),
                Moq.Protected.ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/json")
            });
        return mockHandler;
    }
    
    /// <summary>
    /// Creates a successful OpenAI-compatible API response
    /// </summary>
    private string CreateSuccessResponse(string content)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
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
    /// Property 5: Provider Errors Return Descriptive Messages
    /// **Validates: Requirements 2.3**
    /// Tests that various provider errors return descriptive error messages
    /// </summary>
    [Fact]
    public async Task ProviderErrorsReturnDescriptiveMessages()
    {
        // Define HTTP error scenarios to test
        var errorScenarios = new[]
        {
            // (StatusCode, ErrorType, ExpectedKeywords)
            (System.Net.HttpStatusCode.BadRequest, ErrorType.ValidationError, new[] { "Invalid", "request" }),
            (System.Net.HttpStatusCode.Unauthorized, ErrorType.AuthenticationError, new[] { "Authentication", "denied" }),
            (System.Net.HttpStatusCode.Forbidden, ErrorType.AuthenticationError, new[] { "Authentication", "denied" }),
            (System.Net.HttpStatusCode.NotFound, ErrorType.ProviderError, new[] { "not found", "model", "Endpoint" }),
            ((System.Net.HttpStatusCode)429, ErrorType.RateLimitError, new[] { "Rate limit", "exceeded" }),
            (System.Net.HttpStatusCode.InternalServerError, ErrorType.ProviderError, new[] { "server error" }),
            (System.Net.HttpStatusCode.BadGateway, ErrorType.ProviderError, new[] { "server error" }),
            (System.Net.HttpStatusCode.ServiceUnavailable, ErrorType.ProviderError, new[] { "server error" })
        };

        // Generate random context and prompt for each test
        var contextGen = MessageGenerators.MessageContent();
        var promptGen = MessageGenerators.MessageContent();
        
        // Run approximately 100 iterations (8 scenarios × ~13 iterations each)
        for (int iteration = 0; iteration < 13; iteration++)
        {
            var contexts = contextGen.Sample(size: 10, count: errorScenarios.Length);
            var prompts = promptGen.Sample(size: 10, count: errorScenarios.Length);
            
            for (int i = 0; i < errorScenarios.Length; i++)
            {
                var (statusCode, expectedErrorType, expectedKeywords) = errorScenarios[i];
                var context = contexts[i];
                var prompt = prompts[i];
                
                // Create mock HTTP handler that returns the error status code
                var errorMessage = $"Error occurred for status {(int)statusCode}";
                var mockHandler = CreateMockHttpMessageHandler(
                    statusCode,
                    CreateErrorResponse(errorMessage));
                
                var httpClient = new HttpClient(mockHandler.Object);
                
                // Create provider with mock HTTP client
                var config = new LLMConfiguration
                {
                    ProviderType = ProviderType.Local,
                    ModelIdentifier = "test-model",
                    Endpoint = "http://localhost:11434",
                    TimeoutSeconds = 30,
                    MaxRetries = 0 // No retries for faster test
                };
                
                var provider = new LocalProvider(config, httpClient);
                
                // Call AnalyzeAsync and verify error response
                var response = await provider.AnalyzeAsync(context, prompt);
                
                // Verify response indicates failure
                Assert.False(response.Success, 
                    $"Response should indicate failure for HTTP {(int)statusCode}");
                
                // Verify error is present
                Assert.NotNull(response.Error);
                
                // Verify correct error type
                Assert.Equal(expectedErrorType, response.Error!.Type);
                
                // Verify error message is descriptive and contains expected keywords
                Assert.False(string.IsNullOrEmpty(response.Error.Message),
                    "Error message should be non-empty");
                
                var errorMessageLower = response.Error.Message.ToLowerInvariant();
                var hasExpectedKeyword = expectedKeywords.Any(keyword => 
                    errorMessageLower.Contains(keyword.ToLowerInvariant()));
                
                Assert.True(hasExpectedKeyword,
                    $"Error message should contain one of: {string.Join(", ", expectedKeywords)}. " +
                    $"Got: {response.Error.Message}");
                
                // Verify error message includes contextual information (endpoint or model)
                Assert.True(
                    response.Error.Message.Contains("localhost") || 
                    response.Error.Message.Contains("test-model") ||
                    response.Error.Message.Contains("11434"),
                    "Error message should include contextual information (endpoint or model)");
            }
        }
    }
    
    /// <summary>
    /// Creates an error response in OpenAI-compatible format
    /// </summary>
    private string CreateErrorResponse(string errorMessage)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            error = new
            {
                message = errorMessage,
                type = "invalid_request_error",
                code = "error"
            }
        });
    }

    /// <summary>
    /// Property 8: Session Context Inclusion
    /// Validates: Requirements 3.3
    /// Note: Tests that session history is maintained and accessible
    /// </summary>
    [Fact]
    public void SessionContextInclusion()
    {
        // Run 100 iterations
        var gen = SessionGenerators.MessagesWithRoles();
        var samples = gen.Sample(size: 10, count: 100);
        
        foreach (var messages in samples)
        {
            var manager = new SessionManager();
            var session = manager.CreateSession();
            
            // Add messages to session
            foreach (var (content, role) in messages)
            {
                session.AddMessage(new Message(content, role));
            }
            
            // Verify all messages are included in session
            Assert.Equal(messages.Count, session.Messages.Count);
            
            // Verify messages are in correct order
            for (int i = 0; i < messages.Count; i++)
            {
                Assert.Equal(messages[i].content, session.Messages[i].Content);
                Assert.Equal(messages[i].role, session.Messages[i].Role);
            }
        }
    }

    /// <summary>
    /// Property 11: Network Errors Return Connection Error
    /// **Validates: Requirements 5.2**
    /// Tests that network failures (connection refused, timeout, DNS failure) return ConnectionError type with descriptive messages
    /// </summary>
    [Fact]
    public async Task NetworkErrorsReturnConnectionError()
    {
        // Test scenario 1: Connection timeout
        // Create configurations with very short timeout to simulate timeout errors
        var timeoutGen = Gen.Int32(Hedgehog.Linq.Range.Constant(10000, 65000))
            .Select(port => new LLMConfiguration
            {
                ProviderType = ProviderType.Local,
                ModelIdentifier = "test-model",
                Endpoint = $"http://192.0.2.1:{port}", // TEST-NET-1 address (non-routable, will timeout)
                TimeoutSeconds = 1, // Very short timeout
                MaxRetries = 0
            });

        var timeoutConfigs = timeoutGen.Sample(size: 10, count: 30);

        foreach (var config in timeoutConfigs)
        {
            var httpClient = new HttpClient();
            var provider = new LocalProvider(config, httpClient);
            
            // Test AnalyzeAsync - should timeout and return ConnectionError
            var response = await provider.AnalyzeAsync("test context", "test prompt");
            
            Assert.False(response.Success, "Response should indicate failure for timeout");
            Assert.NotNull(response.Error);
            Assert.Equal(ErrorType.ConnectionError, response.Error!.Type);
            
            var errorMessage = response.Error.Message.ToLowerInvariant();
            Assert.True(
                errorMessage.Contains("timeout") || 
                errorMessage.Contains("timed out") ||
                errorMessage.Contains("connection"),
                $"Error message should mention timeout or connection. Got: {response.Error.Message}");
            
            // Verify error message includes endpoint for context
            Assert.Contains(config.Endpoint ?? "", response.Error.Message, StringComparison.OrdinalIgnoreCase);
        }

        // Test scenario 2: Connection refused
        // Use localhost with random high ports that are unlikely to be in use
        var connectionRefusedGen = Gen.Int32(Hedgehog.Linq.Range.Constant(50000, 65000))
            .Select(port => new LLMConfiguration
            {
                ProviderType = ProviderType.Local,
                ModelIdentifier = "test-model",
                Endpoint = $"http://localhost:{port}",
                TimeoutSeconds = 5,
                MaxRetries = 0
            });

        var connectionRefusedConfigs = connectionRefusedGen.Sample(size: 10, count: 35);

        foreach (var config in connectionRefusedConfigs)
        {
            var httpClient = new HttpClient();
            var provider = new LocalProvider(config, httpClient);
            
            // Test AnalyzeAsync - should get connection refused and return ConnectionError
            var response = await provider.AnalyzeAsync("test context", "test prompt");
            
            Assert.False(response.Success, "Response should indicate failure for connection refused");
            Assert.NotNull(response.Error);
            Assert.Equal(ErrorType.ConnectionError, response.Error!.Type);
            
            var errorMessage = response.Error.Message.ToLowerInvariant();
            Assert.True(
                errorMessage.Contains("connection") || 
                errorMessage.Contains("connect") ||
                errorMessage.Contains("refused") ||
                errorMessage.Contains("failed"),
                $"Error message should mention connection failure. Got: {response.Error.Message}");
            
            // Verify error message includes endpoint for context
            Assert.Contains("localhost", response.Error.Message, StringComparison.OrdinalIgnoreCase);
        }

        // Test scenario 3: DNS failure / invalid hostname
        // Use invalid hostnames that will fail DNS resolution
        var invalidHostnames = new[] 
        { 
            "invalid-hostname-that-does-not-exist-12345.local",
            "nonexistent-domain-xyz-99999.invalid",
            "fake-llm-server-abcdef.test"
        };

        var dnsFailureGen = Gen.Item(invalidHostnames)
            .Select(hostname => new LLMConfiguration
            {
                ProviderType = ProviderType.Local,
                ModelIdentifier = "test-model",
                Endpoint = $"http://{hostname}:8080",
                TimeoutSeconds = 5,
                MaxRetries = 0
            });

        var dnsFailureConfigs = dnsFailureGen.Sample(size: 10, count: 35);

        foreach (var config in dnsFailureConfigs)
        {
            var httpClient = new HttpClient();
            var provider = new LocalProvider(config, httpClient);
            
            // Test AnalyzeAsync - should fail DNS resolution and return ConnectionError
            var response = await provider.AnalyzeAsync("test context", "test prompt");
            
            Assert.False(response.Success, "Response should indicate failure for DNS failure");
            Assert.NotNull(response.Error);
            Assert.Equal(ErrorType.ConnectionError, response.Error!.Type);
            
            var errorMessage = response.Error.Message.ToLowerInvariant();
            Assert.True(
                errorMessage.Contains("connection") || 
                errorMessage.Contains("connect") ||
                errorMessage.Contains("dns") ||
                errorMessage.Contains("resolve") ||
                errorMessage.Contains("host") ||
                errorMessage.Contains("failed"),
                $"Error message should mention connection or DNS failure. Got: {response.Error.Message}");
            
            // Verify error message is descriptive (not just a generic error)
            Assert.True(response.Error.Message.Length > 20,
                "Error message should be descriptive, not just a short generic message");
        }
    }

    /// <summary>
    /// Property 12: Rate Limit Errors Return Rate Limit Error
    /// **Validates: Requirements 5.3**
    /// Tests that rate limit responses (HTTP 429) return RateLimitError type with descriptive messages
    /// </summary>
    [Fact]
    public async Task RateLimitErrorsReturnRateLimitError()
    {
        // Generate random context and prompt for each test (100 iterations)
        var contextGen = MessageGenerators.MessageContent();
        var promptGen = MessageGenerators.MessageContent();
        
        var contexts = contextGen.Sample(size: 10, count: 100);
        var prompts = promptGen.Sample(size: 10, count: 100);
        
        for (int i = 0; i < 100; i++)
        {
            var context = contexts[i];
            var prompt = prompts[i];
            
            // Create mock HTTP handler that returns HTTP 429 (Rate Limit)
            // Vary the error message to test different scenarios
            string rateLimitMessage;
            if (i % 3 == 0)
            {
                rateLimitMessage = "Rate limit exceeded. Please retry after 60 seconds.";
            }
            else if (i % 3 == 1)
            {
                rateLimitMessage = "Too many requests. Quota exceeded for this model.";
            }
            else
            {
                rateLimitMessage = "API rate limit reached. Please slow down your requests.";
            }
            
            var mockHandler = CreateMockHttpMessageHandler(
                (System.Net.HttpStatusCode)429,
                CreateErrorResponse(rateLimitMessage));
            
            var httpClient = new HttpClient(mockHandler.Object);
            
            // Create provider with mock HTTP client
            var config = new LLMConfiguration
            {
                ProviderType = ProviderType.Local,
                ModelIdentifier = $"test-model-{i % 5}",
                Endpoint = "http://localhost:11434",
                TimeoutSeconds = 30,
                MaxRetries = 0 // No retries for faster test
            };
            
            var provider = new LocalProvider(config, httpClient);
            
            // Call AnalyzeAsync and verify rate limit error response
            var response = await provider.AnalyzeAsync(context, prompt);
            
            // Verify response indicates failure
            Assert.False(response.Success, 
                "Response should indicate failure for HTTP 429 rate limit");
            
            // Verify error is present
            Assert.NotNull(response.Error);
            
            // Verify correct error type is RateLimitError
            Assert.Equal(ErrorType.RateLimitError, response.Error!.Type);
            
            // Verify error message is descriptive and contains rate limit keywords
            Assert.False(string.IsNullOrEmpty(response.Error.Message),
                "Error message should be non-empty");
            
            var errorMessageLower = response.Error.Message.ToLowerInvariant();
            Assert.True(
                errorMessageLower.Contains("rate limit") || 
                errorMessageLower.Contains("rate-limit") ||
                errorMessageLower.Contains("ratelimit"),
                $"Error message should contain 'rate limit'. Got: {response.Error.Message}");
            
            // Verify error message includes model identifier for context
            Assert.Contains(config.ModelIdentifier, response.Error.Message, StringComparison.Ordinal);
            
            // Verify error message suggests action (retry, wait, etc.)
            Assert.True(
                errorMessageLower.Contains("retry") || 
                errorMessageLower.Contains("wait") ||
                errorMessageLower.Contains("exceeded"),
                $"Error message should suggest action or explain the issue. Got: {response.Error.Message}");
        }
    }
}
