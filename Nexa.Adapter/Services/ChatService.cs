using Microsoft.Extensions.Caching.Memory;
using Nexa.Adapter.Infrastructure.LLM;
using Nexa.Adapter.Models;

namespace Nexa.Adapter.Services
{
    public class ChatService(IMemoryCache memoryCache, ILLMProvider llmProvider)
    {
        private readonly IMemoryCache _cache=memoryCache;
        private readonly ILLMProvider _llmProvider = llmProvider;
        public async Task<Object> ProcessChat(ChatRequest chat)
        {
            var historyFound = _cache.TryGetValue(chat.SessionId, out var result);
            var message = new List<LlmMessage>();
            if (!historyFound)
            {
                message.Add(new LlmMessage { Role=Role.System, Content=})
            }


        }

    }
}
