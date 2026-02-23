using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime;
using LLMProviderAbstraction.Configuration;
using LLMProviderAbstraction.Interfaces;
using LLMProviderAbstraction.Models;

namespace LLMProviderAbstraction.Providers;

/// <summary>
/// AWS Bedrock implementation of ILLMProvider
/// </summary>
public class BedrockProvider : ILLMProvider
{
    private readonly AmazonBedrockRuntimeClient _client;
    private readonly string _modelId;
    private readonly int _maxRetries;

    /// <summary>
    /// Creates a new BedrockProvider instance with the specified configuration
    /// </summary>
    /// <param name="config">Configuration containing AWS credentials, region, and model identifier</param>
    /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
    /// <exception cref="ArgumentException">Thrown when required configuration values are missing</exception>
    public BedrockProvider(LLMConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.AccessKey))
        {
            throw new ArgumentException("AccessKey is required for Bedrock provider", nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.SecretKey))
        {
            throw new ArgumentException("SecretKey is required for Bedrock provider", nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.ModelIdentifier))
        {
            throw new ArgumentException("ModelIdentifier is required", nameof(config));
        }

        // Initialize AWS credentials
        var credentials = new BasicAWSCredentials(config.AccessKey, config.SecretKey);

        // Get region endpoint (default to us-east-1 if not specified)
        var regionEndpoint = RegionEndpoint.GetBySystemName(config.Region ?? "us-east-1");

        // Initialize Bedrock Runtime client
        _client = new AmazonBedrockRuntimeClient(credentials, regionEndpoint);

        // Store model identifier
        _modelId = config.ModelIdentifier;
        
        // Store max retries for retry logic
        _maxRetries = config.MaxRetries;
    }

    /// <inheritdoc />
    public async Task<LLMResponse> AnalyzeAsync(string context, string prompt, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            try
            {
                // Format the context and prompt for Bedrock Converse API
                var userMessage = string.IsNullOrWhiteSpace(context)
                    ? prompt
                    : $"{context}\n\n{prompt}";

                var request = new ConverseRequest
                {
                    ModelId = _modelId,
                    Messages = new List<Amazon.BedrockRuntime.Model.Message>
                    {
                        new Amazon.BedrockRuntime.Model.Message
                        {
                            Role = "user",
                            Content = new List<ContentBlock>
                            {
                                new ContentBlock { Text = userMessage }
                            }
                        }
                    },
                    InferenceConfig = new InferenceConfiguration
                    {
                        MaxTokens = 2048,
                        Temperature = 0.7f
                    }
                };

                // Send request using Converse API
                var response = await _client.ConverseAsync(request, cancellationToken);

                // Parse response and extract content
                var content = ExtractContentFromResponse(response);

                // Create metadata
                var metadata = new Dictionary<string, object>
                {
                    { "ModelId", _modelId },
                    { "StopReason", response.StopReason?.Value ?? "unknown" }
                };

                if (response.Usage != null)
                {
                    metadata["InputTokens"] = response.Usage.InputTokens;
                    metadata["OutputTokens"] = response.Usage.OutputTokens;
                    metadata["TotalTokens"] = response.Usage.TotalTokens;
                }

                return LLMResponse.CreateSuccess(content, metadata);
            }
            catch (AmazonBedrockRuntimeException ex) when (ex.ErrorCode == "ThrottlingException")
            {
                // Rate limit error
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.RateLimitError,
                    $"Rate limit exceeded for model '{_modelId}'. Please wait before retrying. Error: {ex.Message}",
                    ex
                ));
            }
            catch (AmazonBedrockRuntimeException ex) when (ex.ErrorCode == "ValidationException")
            {
                // Validation error
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.ValidationError,
                    $"Invalid request to Bedrock: {ex.Message}",
                    ex
                ));
            }
            catch (AmazonBedrockRuntimeException ex) when (ex.GetType().Name == "AccessDeniedException" || ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                // Authentication error
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.AuthenticationError,
                    $"Access denied to AWS Bedrock service. Verify your credentials have the necessary permissions. Error: {ex.Message}",
                    ex
                ));
            }
            catch (AmazonServiceException ex) when (ex.ErrorCode == "ResourceNotFoundException")
            {
                // Provider error - model not found
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.ProviderError,
                    $"Model '{_modelId}' not found or not available in the specified region. Error: {ex.Message}",
                    ex
                ));
            }
            catch (AmazonServiceException ex) when (ex.ErrorCode == "ModelNotReadyException")
            {
                // Provider error - model not ready
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.ProviderError,
                    $"Model '{_modelId}' is not ready. Please try again later. Error: {ex.Message}",
                    ex
                ));
            }
            catch (AmazonServiceException ex) when (ex.ErrorCode == "ServiceUnavailableException")
            {
                // Provider error - service unavailable (transient)
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.ProviderError,
                    $"AWS Bedrock service is temporarily unavailable. Error: {ex.Message}",
                    ex
                ));
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                // Network connectivity issue
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.ConnectionError,
                    $"Failed to connect to AWS Bedrock service. Check your network connection. Error: {ex.Message}",
                    ex
                ));
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.ConnectionError,
                    $"Request to AWS Bedrock service timed out. Check your network connection. Error: {ex.Message}",
                    ex
                ));
            }
            catch (AmazonServiceException ex)
            {
                // Other AWS service errors
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.ProviderError,
                    $"AWS Bedrock service error: {ex.ErrorCode} - {ex.Message}",
                    ex
                ));
            }
            catch (Exception ex)
            {
                // Unexpected errors
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.UnknownError,
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
                // Convert session message history to Bedrock message format
                var bedrockMessages = new List<Amazon.BedrockRuntime.Model.Message>();

                // Add existing messages from session history
                foreach (var sessionMessage in session.Messages)
                {
                    // Map MessageRole to Bedrock role string
                    string role = sessionMessage.Role switch
                    {
                        MessageRole.User => "user",
                        MessageRole.Assistant => "assistant",
                        MessageRole.System => "user", // Bedrock doesn't have a system role in messages, treat as user
                        _ => "user"
                    };

                    bedrockMessages.Add(new Amazon.BedrockRuntime.Model.Message
                    {
                        Role = role,
                        Content = new List<ContentBlock>
                        {
                            new ContentBlock { Text = sessionMessage.Content }
                        }
                    });
                }

                // Add new user message to request
                bedrockMessages.Add(new Amazon.BedrockRuntime.Model.Message
                {
                    Role = "user",
                    Content = new List<ContentBlock>
                    {
                        new ContentBlock { Text = message }
                    }
                });

                // Send request with full conversation context
                var request = new ConverseRequest
                {
                    ModelId = _modelId,
                    Messages = bedrockMessages,
                    InferenceConfig = new InferenceConfiguration
                    {
                        MaxTokens = 2048,
                        Temperature = 0.7f
                    }
                };

                var response = await _client.ConverseAsync(request, cancellationToken);

                // Parse response and extract content
                var content = ExtractContentFromResponse(response);

                // Add user message to session
                session.AddMessage(new Models.Message(message, MessageRole.User));

                // Add assistant message to session
                session.AddMessage(new Models.Message(content, MessageRole.Assistant));

                // Create metadata
                var metadata = new Dictionary<string, object>
                {
                    { "ModelId", _modelId },
                    { "StopReason", response.StopReason?.Value ?? "unknown" },
                    { "SessionId", session.SessionId }
                };

                if (response.Usage != null)
                {
                    metadata["InputTokens"] = response.Usage.InputTokens;
                    metadata["OutputTokens"] = response.Usage.OutputTokens;
                    metadata["TotalTokens"] = response.Usage.TotalTokens;
                }

                return LLMResponse.CreateSuccess(content, metadata);
            }
            catch (AmazonBedrockRuntimeException ex) when (ex.ErrorCode == "ThrottlingException")
            {
                // Rate limit error
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.RateLimitError,
                    $"Rate limit exceeded for model '{_modelId}'. Please wait before retrying. Error: {ex.Message}",
                    ex
                ));
            }
            catch (AmazonBedrockRuntimeException ex) when (ex.ErrorCode == "ValidationException")
            {
                // Validation error
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.ValidationError,
                    $"Invalid request to Bedrock: {ex.Message}",
                    ex
                ));
            }
            catch (AmazonBedrockRuntimeException ex) when (ex.GetType().Name == "AccessDeniedException" || ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                // Authentication error
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.AuthenticationError,
                    $"Access denied to AWS Bedrock service. Verify your credentials have the necessary permissions. Error: {ex.Message}",
                    ex
                ));
            }
            catch (AmazonServiceException ex) when (ex.ErrorCode == "ResourceNotFoundException")
            {
                // Provider error - model not found
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.ProviderError,
                    $"Model '{_modelId}' not found or not available in the specified region. Error: {ex.Message}",
                    ex
                ));
            }
            catch (AmazonServiceException ex) when (ex.ErrorCode == "ModelNotReadyException")
            {
                // Provider error - model not ready
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.ProviderError,
                    $"Model '{_modelId}' is not ready. Please try again later. Error: {ex.Message}",
                    ex
                ));
            }
            catch (AmazonServiceException ex) when (ex.ErrorCode == "ServiceUnavailableException")
            {
                // Provider error - service unavailable (transient)
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.ProviderError,
                    $"AWS Bedrock service is temporarily unavailable. Error: {ex.Message}",
                    ex
                ));
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                // Network connectivity issue
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.ConnectionError,
                    $"Failed to connect to AWS Bedrock service. Check your network connection. Error: {ex.Message}",
                    ex
                ));
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.ConnectionError,
                    $"Request to AWS Bedrock service timed out. Check your network connection. Error: {ex.Message}",
                    ex
                ));
            }
            catch (AmazonServiceException ex)
            {
                // Other AWS service errors
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.ProviderError,
                    $"AWS Bedrock service error: {ex.ErrorCode} - {ex.Message}",
                    ex
                ));
            }
            catch (Exception ex)
            {
                // Unexpected errors
                return LLMResponse.CreateError(new LLMError(
                    Models.ErrorType.UnknownError,
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
            // Test connectivity by attempting a minimal converse request
            // This validates credentials, connectivity, and model access
            var request = new ConverseRequest
            {
                ModelId = _modelId,
                Messages = new List<Amazon.BedrockRuntime.Model.Message>
                {
                    new Amazon.BedrockRuntime.Model.Message
                    {
                        Role = "user",
                        Content = new List<ContentBlock>
                        {
                            new ContentBlock { Text = "test" }
                        }
                    }
                },
                InferenceConfig = new InferenceConfiguration
                {
                    MaxTokens = 1,
                    Temperature = 0
                }
            };
            
            await _client.ConverseAsync(request, cancellationToken);
            
            return ValidationResult.CreateSuccess();
        }
        catch (AmazonBedrockRuntimeException ex) when (ex.GetType().Name == "AccessDeniedException")
        {
            // Authentication/authorization failure
            return ValidationResult.CreateFailure(new[]
            {
                $"Authentication failed: Access denied to AWS Bedrock service. Verify your credentials have the necessary permissions. Error: {ex.Message}"
            });
        }
        catch (AmazonBedrockRuntimeException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            // Authentication/authorization failure
            return ValidationResult.CreateFailure(new[]
            {
                $"Authentication failed: Access forbidden. Verify your AWS credentials and IAM permissions for Bedrock. Error: {ex.Message}"
            });
        }
        catch (AmazonBedrockRuntimeException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Authentication failure
            return ValidationResult.CreateFailure(new[]
            {
                $"Authentication failed: Invalid AWS credentials. Verify your AccessKey and SecretKey are correct. Error: {ex.Message}"
            });
        }
        catch (AmazonServiceException ex) when (ex.ErrorCode == "UnrecognizedClientException")
        {
            // Invalid credentials
            return ValidationResult.CreateFailure(new[]
            {
                $"Authentication failed: AWS credentials are invalid or malformed. Error: {ex.Message}"
            });
        }
        catch (AmazonServiceException ex) when (ex.ErrorCode == "InvalidSignatureException")
        {
            // Invalid secret key
            return ValidationResult.CreateFailure(new[]
            {
                $"Authentication failed: Invalid AWS secret key. Verify your SecretKey is correct. Error: {ex.Message}"
            });
        }
        catch (AmazonServiceException ex) when (ex.ErrorCode == "SignatureDoesNotMatch")
        {
            // Signature mismatch
            return ValidationResult.CreateFailure(new[]
            {
                $"Authentication failed: AWS signature does not match. Verify your SecretKey is correct. Error: {ex.Message}"
            });
        }
        catch (AmazonServiceException ex) when (ex.ErrorCode == "ValidationException")
        {
            // Validation error
            return ValidationResult.CreateFailure(new[]
            {
                $"Validation error: {ex.Message}"
            });
        }
        catch (AmazonServiceException ex) when (ex.ErrorCode == "ResourceNotFoundException")
        {
            // Resource not found - model might not exist or region doesn't support it
            return ValidationResult.CreateFailure(new[]
            {
                $"Model not found: The specified model '{_modelId}' may not exist or is not available in the specified region. Error: {ex.Message}"
            });
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            // Network connectivity issue
            return ValidationResult.CreateFailure(new[]
            {
                $"Connection failed: Unable to connect to AWS Bedrock service. Check your network connection and ensure the service is accessible. Error: {ex.Message}"
            });
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout
            return ValidationResult.CreateFailure(new[]
            {
                $"Connection timeout: Request to AWS Bedrock service timed out. Check your network connection. Error: {ex.Message}"
            });
        }
        catch (AmazonServiceException ex)
        {
            // Other AWS service errors
            return ValidationResult.CreateFailure(new[]
            {
                $"AWS service error: {ex.ErrorCode} - {ex.Message}"
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
        return error?.Type is Models.ErrorType.ConnectionError or Models.ErrorType.RateLimitError;
    }

    /// <summary>
    /// Extracts text content from a Bedrock Converse API response
    /// </summary>
    private string ExtractContentFromResponse(ConverseResponse response)
    {
        if (response.Output?.Message?.Content == null || response.Output.Message.Content.Count == 0)
        {
            return string.Empty;
        }

        // Concatenate all text blocks from the response
        var textBlocks = response.Output.Message.Content
            .Where(block => !string.IsNullOrEmpty(block.Text))
            .Select(block => block.Text);

        return string.Join("\n", textBlocks);
    }
}
