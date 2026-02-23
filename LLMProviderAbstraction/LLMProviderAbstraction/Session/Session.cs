using LLMProviderAbstraction.Models;

namespace LLMProviderAbstraction.Session;

/// <summary>
/// Represents a conversation session that maintains message history
/// </summary>
public class Session
{
    private readonly List<Message> _messages = new();
    
    /// <summary>
    /// Unique identifier for the session
    /// </summary>
    public string SessionId { get; }
    
    /// <summary>
    /// Timestamp when the session was created
    /// </summary>
    public DateTime CreatedAt { get; }
    
    /// <summary>
    /// Timestamp when the session was last accessed
    /// </summary>
    public DateTime LastAccessedAt { get; private set; }
    
    /// <summary>
    /// Read-only collection of messages in the session
    /// </summary>
    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();
    
    /// <summary>
    /// Creates a new session with the specified ID
    /// </summary>
    /// <param name="sessionId">Unique identifier for the session</param>
    public Session(string sessionId)
    {
        SessionId = sessionId;
        CreatedAt = DateTime.UtcNow;
        LastAccessedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Adds a message to the session and updates the last accessed timestamp
    /// </summary>
    /// <param name="message">The message to add</param>
    public void AddMessage(Message message)
    {
        _messages.Add(message);
        LastAccessedAt = DateTime.UtcNow;
    }
}
