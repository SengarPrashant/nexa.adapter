namespace LLMProviderAbstraction.Models;

/// <summary>
/// Represents a single message in a conversation
/// </summary>
public class Message
{
    /// <summary>
    /// The content of the message
    /// </summary>
    public string Content { get; }
    
    /// <summary>
    /// The role of the message sender
    /// </summary>
    public MessageRole Role { get; }
    
    /// <summary>
    /// The timestamp when the message was created
    /// </summary>
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// Creates a new message
    /// </summary>
    /// <param name="content">The message content</param>
    /// <param name="role">The role of the message sender</param>
    public Message(string content, MessageRole role)
    {
        Content = content;
        Role = role;
        Timestamp = DateTime.UtcNow;
    }
}
