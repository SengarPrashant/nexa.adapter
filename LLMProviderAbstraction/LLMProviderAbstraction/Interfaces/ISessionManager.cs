using LLMProviderAbstraction.Models;

namespace LLMProviderAbstraction.Interfaces;

/// <summary>
/// Manages conversation sessions and their message history
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Creates a new conversation session
    /// </summary>
    /// <param name="sessionId">Optional session identifier. If null, a new GUID will be generated.</param>
    /// <returns>A new Session instance</returns>
    LLMProviderAbstraction.Session.Session CreateSession(string? sessionId = null);
    
    /// <summary>
    /// Retrieves an existing session by ID
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session to retrieve</param>
    /// <returns>The Session if found, otherwise null</returns>
    LLMProviderAbstraction.Session.Session? GetSession(string sessionId);
    
    /// <summary>
    /// Retrieves all messages in a session
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session</param>
    /// <returns>Read-only list of messages in the session, or empty list if session not found</returns>
    IReadOnlyList<Message> GetSessionHistory(string sessionId);
}
