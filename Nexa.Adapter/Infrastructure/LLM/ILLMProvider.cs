using Amazon.BedrockRuntime.Model;
using Nexa.Adapter.Models;
using Nexa.Adapter.Services;
using static Nexa.Adapter.Infrastructure.LLM.AwsResponseParser;

namespace Nexa.Adapter.Infrastructure.LLM
{
    public interface ILLMProvider
    {
        Task<LlmAnalysisResponse> Analyze(List<LlmMessage> messages);
        Task<NexaLlmResponse> CompleteChat(List<LlmMessage> messages, IEnumerable<ITool>? toolSpecs = null);
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
        T Parse<T>(string rawResponse, string? extractMethod="");
        (List<ToolUse>? toolUse, string? text) ExtractToolOrText(string json);
        string PrependThinkingToResponse(string rawText);
    }
}
