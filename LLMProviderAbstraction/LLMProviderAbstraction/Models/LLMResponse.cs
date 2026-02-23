namespace LLMProviderAbstraction.Models;

/// <summary>
/// Represents the response from an LLM operation
/// </summary>
public class LLMResponse
{
    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool Success { get; }
    
    /// <summary>
    /// The response content (null if operation failed)
    /// </summary>
    public string? Content { get; }
    
    /// <summary>
    /// Error information (null if operation succeeded)
    /// </summary>
    public LLMError? Error { get; }
    
    /// <summary>
    /// Additional metadata about the response
    /// </summary>
    public Dictionary<string, object> Metadata { get; }
    
    /// <summary>
    /// Private constructor to enforce factory method usage
    /// </summary>
    private LLMResponse(bool success, string? content, LLMError? error, Dictionary<string, object> metadata)
    {
        Success = success;
        Content = content;
        Error = error;
        Metadata = metadata;
    }
    
    /// <summary>
    /// Creates a successful response
    /// </summary>
    /// <param name="content">The response content</param>
    /// <param name="metadata">Optional metadata</param>
    /// <returns>A successful LLMResponse</returns>
    public static LLMResponse CreateSuccess(string content, Dictionary<string, object>? metadata = null)
    {
        return new LLMResponse(true, content, null, metadata ?? new Dictionary<string, object>());
    }
    
    /// <summary>
    /// Creates an error response
    /// </summary>
    /// <param name="error">The error information</param>
    /// <returns>An error LLMResponse</returns>
    public static LLMResponse CreateError(LLMError error)
    {
        return new LLMResponse(false, null, error, new Dictionary<string, object>());
    }
}
