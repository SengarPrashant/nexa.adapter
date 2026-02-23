using Microsoft.Extensions.Caching.Memory;
using Nexa.Adapter.Infrastructure.LLM;
using Nexa.Adapter.Models;

namespace Nexa.Adapter.Services
{
    public interface IChatService
    {
        public Task<NexaLlmResponse> ProcessChat(ChatRequest chat);
    }
    public class ChatService(IMemoryCache memoryCache, ILLMProvider llmProvider, ILLMResponseParser responseParser, IPromptBuilder promptBuilder, IEnumerable<ITool> tools) : IChatService
    {
        private readonly IMemoryCache _cache=memoryCache;
        private readonly ILLMProvider _llmProvider = llmProvider;
        private readonly ILLMResponseParser _llmParser = responseParser;
        private readonly IPromptBuilder _promptBuilder=promptBuilder;
        private readonly IEnumerable<ITool> _tools = tools ?? new List<ITool>();
        public async Task<NexaLlmResponse> ProcessChat(ChatRequest chat)
        {
            try
            {
                var historyFound = string.IsNullOrEmpty(chat.SessionId) ? false : _cache.TryGetValue<List<LlmMessage>>(chat.SessionId.Trim(), out var result);
                var message = new List<LlmMessage>();
                if (!historyFound)
                {
                    var systemPromt = _promptBuilder.BuildFollowUpPrompt(chat.InitialContext);
                    message.Add(new LlmMessage { Role = Role.System, Content = systemPromt });
                }
                message.Add(new LlmMessage { Role = Role.User, Content = chat.Content });
                var AiResponse = await _llmProvider.CompleteChat(message, _tools);
                AiResponse.SessionId = string.IsNullOrEmpty(chat.SessionId) ? Guid.NewGuid().ToString() : chat.SessionId.Trim();
                return AiResponse;
            }
            catch (Exception ex)
            {
                // TO DO This is temporary fallback this need refactored
              return new NexaLlmResponse
                {
                    Response = "NEXA is unable to provide an answer due to insufficient data or lack of permissions to analyze this request.",
                    ResponseType = "General",
                    ConfidenceStatement = "Not Available",
                    EvidenceReference = new List<string>()
                };
                
            }
        }

    }
}
