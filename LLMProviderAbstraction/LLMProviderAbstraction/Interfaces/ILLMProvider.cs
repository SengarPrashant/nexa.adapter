using LLMProviderAbstraction.Models;

namespace LLMProviderAbstraction.Interfaces;

/// <summary>
/// Defines the contract for LLM provider implementations
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// Sends a context-based analysis request to the LLM
    /// </summary>
    /// <param name="context">Input data or information provided to the LLM for analysis</param>
    /// <param name="prompt">The prompt or question to ask about the context</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation, containing the LLM response</returns>
    Task<LLMResponse> AnalyzeAsync(string context, string prompt, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a message within a session context
    /// </summary>
    /// <param name="session">The conversation session containing message history</param>
    /// <param name="message">The message to send</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation, containing the LLM response</returns>
    Task<LLMResponse> SendMessageAsync(LLMProviderAbstraction.Session.Session session, string message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates the provider configuration and connectivity
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation, containing the validation result</returns>
    Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default);
}
