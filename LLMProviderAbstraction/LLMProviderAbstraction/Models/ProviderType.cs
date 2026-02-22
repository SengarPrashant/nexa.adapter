namespace LLMProviderAbstraction.Models;

/// <summary>
/// Represents the type of LLM provider
/// </summary>
public enum ProviderType
{
    /// <summary>
    /// Amazon Bedrock cloud-based provider
    /// </summary>
    Bedrock,
    
    /// <summary>
    /// Locally hosted LLM provider
    /// </summary>
    Local
}
