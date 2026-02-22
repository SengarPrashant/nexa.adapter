using LLMProviderAbstraction.Models;

namespace LLMProviderAbstraction.Configuration;

/// <summary>
/// Configuration settings for LLM provider initialization
/// </summary>
public class LLMConfiguration
{
    /// <summary>
    /// The type of LLM provider (Bedrock or Local)
    /// </summary>
    public ProviderType ProviderType { get; set; }
    
    /// <summary>
    /// The model identifier to use (e.g., "anthropic.claude-3-sonnet-20240229-v1:0" for Bedrock or "llama2" for local)
    /// </summary>
    public string ModelIdentifier { get; set; } = string.Empty;
    
    /// <summary>
    /// Access key for cloud-based providers (required for Bedrock)
    /// </summary>
    public string? AccessKey { get; set; }
    
    /// <summary>
    /// Secret key for cloud-based providers (required for Bedrock)
    /// </summary>
    public string? SecretKey { get; set; }
    
    /// <summary>
    /// AWS region for cloud-based providers (optional, defaults to us-east-1 for Bedrock)
    /// </summary>
    public string? Region { get; set; }
    
    /// <summary>
    /// Endpoint URL for locally hosted providers (required for Local provider type)
    /// </summary>
    public string? Endpoint { get; set; }
    
    /// <summary>
    /// Request timeout in seconds (default: 30)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Maximum number of retry attempts for transient errors (default: 3)
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Validates the configuration and returns a result indicating success or failure with error messages
    /// </summary>
    /// <returns>A ValidationResult indicating whether the configuration is valid</returns>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        // Validate ModelIdentifier is non-empty
        if (string.IsNullOrWhiteSpace(ModelIdentifier))
        {
            errors.Add("ModelIdentifier is required and cannot be empty");
        }

        // Provider-specific validation
        if (ProviderType == ProviderType.Bedrock)
        {
            // Validate AccessKey and SecretKey are provided for Bedrock
            if (string.IsNullOrWhiteSpace(AccessKey))
            {
                errors.Add("AccessKey is required for Bedrock provider");
            }

            if (string.IsNullOrWhiteSpace(SecretKey))
            {
                errors.Add("SecretKey is required for Bedrock provider");
            }
        }
        else if (ProviderType == ProviderType.Local)
        {
            // Validate Endpoint is provided for Local
            if (string.IsNullOrWhiteSpace(Endpoint))
            {
                errors.Add("Endpoint is required for Local provider");
            }
            else if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
            {
                errors.Add("Endpoint must be a valid absolute URI");
            }
        }

        // Validate TimeoutSeconds > 0
        if (TimeoutSeconds <= 0)
        {
            errors.Add("TimeoutSeconds must be greater than 0");
        }

        // Validate MaxRetries >= 0
        if (MaxRetries < 0)
        {
            errors.Add("MaxRetries must be greater than or equal to 0");
        }

        return errors.Any() 
            ? ValidationResult.CreateFailure(errors) 
            : ValidationResult.CreateSuccess();
    }
}
