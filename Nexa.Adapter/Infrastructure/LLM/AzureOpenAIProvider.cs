
using Amazon.BedrockRuntime.Model;
using Nexa.Adapter.Models;
using Nexa.Adapter.Services;

namespace Nexa.Adapter.Infrastructure.LLM
{
    public class AzureOpenAIProvider : ILLMProvider
    {
        public Task<LlmAnalysisResponse> Analyze(List<LlmMessage> messages)
        {
            throw new NotImplementedException();
        }

        Task<NexaLlmResponse> ILLMProvider.CompleteChat(List<LlmMessage> messages, IEnumerable<ITool>? lstTools)
        {
            throw new NotImplementedException();
        }
    }
}
