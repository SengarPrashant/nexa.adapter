using Nexa.Adapter.Models;

namespace Nexa.Adapter.Infrastructure.LLM
{
    public interface ILLMProvider
    {
        Task<LlmAnalysisResponse> Analyze(List<LlmMessage> messages);
        Task<string> CompleteChat(List<LlmMessage> messages);
    }

    public class LlmMessage
    {
        public Role Role { get; set; }
        public string Content { get; set; }
    }
    public enum Role{
        System,
        User,
        Assistant
    }
    public interface ILLMResponseParser
    {
        T Parse<T>(string rawResponse);
    }
}
