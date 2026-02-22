using System.Collections.Concurrent;
using LLMProviderAbstraction.Interfaces;
using LLMProviderAbstraction.Models;

namespace LLMProviderAbstraction.Session;

/// <summary>
/// Manages conversation sessions with thread-safe storage
/// </summary>
public class SessionManager : ISessionManager
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();
    
    /// <summary>
    /// Creates a new conversation session
    /// </summary>
    /// <param name="sessionId">Optional session identifier. If null, a new GUID will be generated.</param>
    /// <returns>A new Session instance</returns>
    public Session CreateSession(string? sessionId = null)
    {
        sessionId ??= Guid.NewGuid().ToString();
        var session = new Session(sessionId);
        _sessions.TryAdd(sessionId, session);
        return session;
    }
    
    /// <summary>
    /// Retrieves an existing session by ID
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session to retrieve</param>
    /// <returns>The Session if found, otherwise null</returns>
    public Session? GetSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }
    
    /// <summary>
    /// Retrieves all messages in a session
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session</param>
    /// <returns>Read-only list of messages in the session, or empty list if session not found</returns>
    public IReadOnlyList<Message> GetSessionHistory(string sessionId)
    {
        var session = GetSession(sessionId);
        return session?.Messages ?? Array.Empty<Message>();
    }
}
