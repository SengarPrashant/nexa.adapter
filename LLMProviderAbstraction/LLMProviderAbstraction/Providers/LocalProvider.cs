using System.Text;
using System.Text.Json;
using LLMProviderAbstraction.Configuration;
using LLMProviderAbstraction.Interfaces;
using LLMProviderAbstraction.Models;

namespace LLMProviderAbstraction.Providers;

/// <summary>
/// Local provider implementation of ILLMProvider for self-hosted models accessible via HTTP endpoints
/// </summary>
public class LocalProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _modelId;
    private readonly int _maxRetries;

    /// <summary>
    /// Creates a new LocalProvider instance with the specified configuration and HttpClient
    /// </summary>
    /// <param name="config">Configuration containing endpoint and model identifier</param>
    /// <param name="httpClient">HttpClient instance for making HTTP requests</param>
    /// <exception cref="ArgumentNullException">Thrown when config or httpClient is null</exception>
    /// <exception cref="ArgumentException">Thrown when required configuration values are missing</exception>
    public LocalProvider(LLMConfiguration config, HttpClient httpClient)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (httpClient == null)
        {
            throw new ArgumentNullException(nameof(httpClient));
        }

        if (string.IsNullOrWhiteSpace(config.Endpoint))
        {
            throw new ArgumentException("Endpoint is required for local provider", nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.ModelIdentifier))
        {
            throw new ArgumentException("ModelIdentifier is required", nameof(config));
        }

        _httpClient = httpClient;
        _endpoint = config.Endpoint;
        _modelId = config.ModelIdentifier;
        _maxRetries = config.MaxRetries;

        // Set timeout from configuration
        _httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
    }

    /// <inheritdoc />
    public async Task<LLMResponse> AnalyzeAsync(string context, string prompt, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            try
            {
                // Format the context and prompt for OpenAI-compatible API
                var userMessage = string.IsNullOrWhiteSpace(context)
                    ? prompt
                    : $"{context}\n\n{prompt}";

                // Build the request payload for /v1/chat/completions endpoint
                var requestPayload = new
                {
                    model = _modelId,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = userMessage
                        }
                    },
                    temperature = 0.7,
                    max_tokens = 2048
                };

                // Serialize to JSON
                var jsonContent = JsonSerializer.Serialize(requestPayload);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send POST request to /v1/chat/completions
                var endpoint = _endpoint.TrimEnd('/') + "/v1/chat/completions";
                var response = await _httpClient.PostAsync(endpoint, httpContent, cancellationToken);

                // Handle HTTP errors
                if (!response.IsSuccessStatusCode)
                {
                    return await HandleHttpErrorAsync(response);
                }

                // Parse JSON response
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                using var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                // Extract content from response
                var content = ExtractContentFromResponse(root);

                // Create metadata
                var metadata = new Dictionary<string, object>
                {
                    { "ModelId", _modelId },
                    { "Endpoint", _endpoint }
                };

                // Add usage information if available
                if (root.TryGetProperty("usage", out var usage))
                {
                    if (usage.TryGetProperty("prompt_tokens", out var promptTokens))
                        metadata["InputTokens"] = promptTokens.GetInt32();
                    if (usage.TryGetProperty("completion_tokens", out var completionTokens))
                        metadata["OutputTokens"] = completionTokens.GetInt32();
                    if (usage.TryGetProperty("total_tokens", out var totalTokens))
                        metadata["TotalTokens"] = totalTokens.GetInt32();
                }

                return LLMResponse.CreateSuccess(content, metadata);
            }
            catch (HttpRequestException ex)
            {
                // Network connectivity issues
                return LLMResponse.CreateError(new LLMError(
                    ErrorType.ConnectionError,
                    $"Failed to connect to local provider at {_endpoint}. Ensure the server is running and accessible. Error: {ex.Message}",
                    ex
                ));
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout
                return LLMResponse.CreateError(new LLMError(
                    ErrorType.ConnectionError,
                    $"Request to local provider at {_endpoint} timed out after {_httpClient.Timeout.TotalSeconds} seconds. Check your network connection and ensure the server is responsive.",
                    ex
                ));
            }
            catch (JsonException ex)
            {
                // JSON parsing error
                return LLMResponse.CreateError(new LLMError(
                    ErrorType.ProviderError,
                    $"Failed to parse response from local provider at {_endpoint}. The response may not be in the expected format. Error: {ex.Message}",
                    ex
                ));
            }
            catch (Exception ex)
            {
                // Unexpected errors
                return LLMResponse.CreateError(new LLMError(
                    ErrorType.UnknownError,
                    $"Unexpected error during analysis: {ex.Message}",
                    ex
                ));
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<LLMResponse> SendMessageAsync(Session.Session session, string message, CancellationToken cancellationToken = default)
    {
        if (session == null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be null or empty", nameof(message));
        }

        return await ExecuteWithRetryAsync(async () =>
        {
            try
            {
                // Convert session message history to OpenAI message format
                var messages = new List<object>();

                // Add existing messages from session history
                foreach (var sessionMessage in session.Messages)
                {
                    // Map MessageRole to OpenAI role string
                    string role = sessionMessage.Role switch
                    {
                        MessageRole.User => "user",
                        MessageRole.Assistant => "assistant",
                        MessageRole.System => "system",
                        _ => "user"
                    };

                    messages.Add(new
                    {
                        role = role,
                        content = sessionMessage.Content
                    });
                }

                // Add new user message to request
                messages.Add(new
                {
                    role = "user",
                    content = message
                });

                // Build the request payload for /v1/chat/completions endpoint
                var requestPayload = new
                {
                    model = _modelId,
                    messages = messages,
                    temperature = 0.7,
                    max_tokens = 2048
                };

                // Serialize to JSON
                var jsonContent = JsonSerializer.Serialize(requestPayload);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send POST request to /v1/chat/completions
                var endpoint = _endpoint.TrimEnd('/') + "/v1/chat/completions";
                var response = await _httpClient.PostAsync(endpoint, httpContent, cancellationToken);

                // Handle HTTP errors
                if (!response.IsSuccessStatusCode)
                {
                    return await HandleHttpErrorAsync(response);
                }

                // Parse JSON response
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                using var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                // Extract content from response
                var content = ExtractContentFromResponse(root);

                // Add user message to session
                session.AddMessage(new Message(message, MessageRole.User));

                // Add assistant message to session
                session.AddMessage(new Message(content, MessageRole.Assistant));

                // Create metadata
                var metadata = new Dictionary<string, object>
                {
                    { "ModelId", _modelId },
                    { "Endpoint", _endpoint },
                    { "SessionId", session.SessionId }
                };

                // Add usage information if available
                if (root.TryGetProperty("usage", out var usage))
                {
                    if (usage.TryGetProperty("prompt_tokens", out var promptTokens))
                        metadata["InputTokens"] = promptTokens.GetInt32();
                    if (usage.TryGetProperty("completion_tokens", out var completionTokens))
                        metadata["OutputTokens"] = completionTokens.GetInt32();
                    if (usage.TryGetProperty("total_tokens", out var totalTokens))
                        metadata["TotalTokens"] = totalTokens.GetInt32();
                }

                return LLMResponse.CreateSuccess(content, metadata);
            }
            catch (HttpRequestException ex)
            {
                // Network connectivity issues
                return LLMResponse.CreateError(new LLMError(
                    ErrorType.ConnectionError,
                    $"Failed to connect to local provider at {_endpoint}. Ensure the server is running and accessible. Error: {ex.Message}",
                    ex
                ));
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout
                return LLMResponse.CreateError(new LLMError(
                    ErrorType.ConnectionError,
                    $"Request to local provider at {_endpoint} timed out after {_httpClient.Timeout.TotalSeconds} seconds. Check your network connection and ensure the server is responsive.",
                    ex
                ));
            }
            catch (JsonException ex)
            {
                // JSON parsing error
                return LLMResponse.CreateError(new LLMError(
                    ErrorType.ProviderError,
                    $"Failed to parse response from local provider at {_endpoint}. The response may not be in the expected format. Error: {ex.Message}",
                    ex
                ));
            }
            catch (Exception ex)
            {
                // Unexpected errors
                return LLMResponse.CreateError(new LLMError(
                    ErrorType.UnknownError,
                    $"Unexpected error during message send: {ex.Message}",
                    ex
                ));
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test endpoint connectivity with a simple HTTP request
            // We'll try to access the models endpoint which is typically available on OpenAI-compatible APIs
            var testEndpoint = _endpoint.TrimEnd('/') + "/v1/models";

            var response = await _httpClient.GetAsync(testEndpoint, cancellationToken);

            // If we get any response (even 404), it means the server is reachable
            // We're primarily testing connectivity here
            if (response.IsSuccessStatusCode || 
                response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
            {
                return ValidationResult.CreateSuccess();
            }

            // Handle specific HTTP error codes
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return ValidationResult.CreateFailure(new[]
                {
                    $"Authentication failed: Access denied to local provider at {_endpoint}. Verify your credentials or API key."
                });
            }

            // Other HTTP errors
            return ValidationResult.CreateFailure(new[]
            {
                $"Connection failed: Local provider at {_endpoint} returned HTTP {(int)response.StatusCode} ({response.StatusCode}). Ensure the server is running and accessible."
            });
        }
        catch (HttpRequestException ex)
        {
            // Network connectivity issues - connection refused, DNS failure, etc.
            string errorMessage;

            if (ex.InnerException is System.Net.Sockets.SocketException socketEx)
            {
                errorMessage = socketEx.SocketErrorCode switch
                {
                    System.Net.Sockets.SocketError.ConnectionRefused => 
                        $"Connection refused: Unable to connect to local provider at {_endpoint}. Ensure the server is running and accessible.",
                    System.Net.Sockets.SocketError.HostNotFound => 
                        $"DNS failure: Unable to resolve hostname for {_endpoint}. Check the endpoint address.",
                    System.Net.Sockets.SocketError.TimedOut => 
                        $"Connection timeout: Unable to connect to local provider at {_endpoint}. Check your network connection and ensure the server is accessible.",
                    _ => 
                        $"Network error: Unable to connect to local provider at {_endpoint}. {ex.Message}"
                };
            }
            else
            {
                errorMessage = $"Connection failed: Unable to connect to local provider at {_endpoint}. Ensure the server is running and accessible. Error: {ex.Message}";
            }

            return ValidationResult.CreateFailure(new[] { errorMessage });
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout - request took too long
            return ValidationResult.CreateFailure(new[]
            {
                $"Connection timeout: Request to local provider at {_endpoint} timed out after {_httpClient.Timeout.TotalSeconds} seconds. Check your network connection and ensure the server is responsive."
            });
        }
        catch (TaskCanceledException)
        {
            // User-requested cancellation
            return ValidationResult.CreateFailure(new[]
            {
                "Validation was cancelled."
            });
        }
        catch (Exception ex)
        {
            // Unexpected errors
            return ValidationResult.CreateFailure(new[]
            {
                $"Unexpected error during validation: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Executes an operation with exponential backoff retry logic for transient errors
    /// </summary>
    private async Task<LLMResponse> ExecuteWithRetryAsync(
        Func<Task<LLMResponse>> operation,
        CancellationToken cancellationToken)
    {
        LLMResponse? lastResponse = null;

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            lastResponse = await operation();

            // If successful or non-transient error, return immediately
            if (lastResponse.Success || !IsTransientError(lastResponse.Error))
            {
                return lastResponse;
            }

            // If we have more retries left, wait with exponential backoff
            if (attempt < _maxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay, cancellationToken);
            }
        }

        // Return the last error after all retries exhausted
        return lastResponse!;
    }

    /// <summary>
    /// Determines if an error is transient and should be retried
    /// </summary>
    private bool IsTransientError(LLMError? error)
    {
        return error?.Type is ErrorType.ConnectionError or ErrorType.RateLimitError;
    }

    /// <summary>
    /// Handles HTTP error responses and maps them to appropriate LLMError types
    /// </summary>
    private async Task<LLMResponse> HandleHttpErrorAsync(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        var reasonPhrase = response.ReasonPhrase ?? "Unknown error";
        
        // Try to read error details from response body
        string errorDetails = string.Empty;
        try
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                // Try to parse JSON error response
                using var jsonDoc = JsonDocument.Parse(responseBody);
                if (jsonDoc.RootElement.TryGetProperty("error", out var errorElement))
                {
                    if (errorElement.TryGetProperty("message", out var messageElement))
                    {
                        errorDetails = messageElement.GetString() ?? string.Empty;
                    }
                    else if (errorElement.ValueKind == JsonValueKind.String)
                    {
                        errorDetails = errorElement.GetString() ?? string.Empty;
                    }
                }
            }
        }
        catch (Exception)
        {
            // Ignore errors when trying to parse error details
        }

        var errorMessage = string.IsNullOrWhiteSpace(errorDetails)
            ? $"Local provider at {_endpoint} returned HTTP {statusCode} ({reasonPhrase})."
            : $"Local provider at {_endpoint} returned HTTP {statusCode} ({reasonPhrase}). Error: {errorDetails}";

        // Map HTTP status codes to error types
        return statusCode switch
        {
            429 => LLMResponse.CreateError(new LLMError(
                ErrorType.RateLimitError,
                $"Rate limit exceeded for model '{_modelId}'. Please wait before retrying. {errorMessage}"
            )),
            401 or 403 => LLMResponse.CreateError(new LLMError(
                ErrorType.AuthenticationError,
                $"Authentication failed: Access denied to local provider at {_endpoint}. Verify your credentials or API key. {errorMessage}"
            )),
            400 => LLMResponse.CreateError(new LLMError(
                ErrorType.ValidationError,
                $"Invalid request to local provider: {errorMessage}"
            )),
            404 => LLMResponse.CreateError(new LLMError(
                ErrorType.ProviderError,
                $"Endpoint not found or model '{_modelId}' not available at {_endpoint}. {errorMessage}"
            )),
            >= 500 and < 600 => LLMResponse.CreateError(new LLMError(
                ErrorType.ProviderError,
                $"Local provider server error: {errorMessage}"
            )),
            _ => LLMResponse.CreateError(new LLMError(
                ErrorType.ProviderError,
                errorMessage
            ))
        };
    }

    /// <summary>
    /// Extracts text content from an OpenAI-compatible API response
    /// </summary>
    private string ExtractContentFromResponse(JsonElement root)
    {
        // OpenAI format: response.choices[0].message.content
        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var firstChoice = choices[0];
            if (firstChoice.TryGetProperty("message", out var message))
            {
                if (message.TryGetProperty("content", out var content))
                {
                    return content.GetString() ?? string.Empty;
                }
            }
        }

        return string.Empty;
    }

}
