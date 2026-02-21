
using Nexa.Adapter.Models;

namespace Nexa.Adapter.Infrastructure.LLM
{
    public class AzureOpenAIProvider : ILLMProvider
    {
       
        public Task<LlmAnalysisResponse> CompleteChat(List<LlmMessage> messages)
        {
            throw new NotImplementedException();
        }
    }
}
