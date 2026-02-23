namespace LLMProviderAbstraction.Models;

/// <summary>
/// Represents an error that occurred during LLM operations
/// </summary>
public class LLMError
{
    /// <summary>
    /// The type of error
    /// </summary>
    public ErrorType Type { get; }
    
    /// <summary>
    /// A descriptive error message
    /// </summary>
    public string Message { get; }
    
    /// <summary>
    /// The underlying exception, if any
    /// </summary>
    public Exception? InnerException { get; }
    
    /// <summary>
    /// Creates a new LLM error
    /// </summary>
    /// <param name="type">The error type</param>
    /// <param name="message">A descriptive error message</param>
    /// <param name="innerException">The underlying exception, if any</param>
    public LLMError(ErrorType type, string message, Exception? innerException = null)
    {
        Type = type;
        Message = message;
        InnerException = innerException;
    }
}
