
using Nexa.Adapter.Models;

namespace Nexa.Adapter.Infrastructure.LLM
{
    public class AzureOpenAIProvider : ILLMProvider
    {
        public Task<LlmAnalysisResponse> Analyze(List<LlmMessage> messages)
        {
            throw new NotImplementedException();
        }

        Task<string> ILLMProvider.CompleteChat(List<LlmMessage> messages)
        {
            throw new NotImplementedException();
        }
    }
}
