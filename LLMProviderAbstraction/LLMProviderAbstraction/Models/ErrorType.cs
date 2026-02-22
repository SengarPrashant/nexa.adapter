namespace LLMProviderAbstraction.Models;

/// <summary>
/// Represents the type of error that occurred
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Configuration or input validation error
    /// </summary>
    ValidationError,
    
    /// <summary>
    /// Network connectivity error
    /// </summary>
    ConnectionError,
    
    /// <summary>
    /// Authentication or authorization error
    /// </summary>
    AuthenticationError,
    
    /// <summary>
    /// Rate limit or quota exceeded error
    /// </summary>
    RateLimitError,
    
    /// <summary>
    /// Provider-specific error (model not found, malformed request, etc.)
    /// </summary>
    ProviderError,
    
    /// <summary>
    /// Unknown or unexpected error
    /// </summary>
    UnknownError
}
