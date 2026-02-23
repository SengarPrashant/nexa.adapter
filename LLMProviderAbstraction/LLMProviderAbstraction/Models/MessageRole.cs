namespace LLMProviderAbstraction.Models;

/// <summary>
/// Represents the role of a message in a conversation
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// Message from the user
    /// </summary>
    User,
    
    /// <summary>
    /// Message from the AI assistant
    /// </summary>
    Assistant,
    
    /// <summary>
    /// System message for context or instructions
    /// </summary>
    System
}
