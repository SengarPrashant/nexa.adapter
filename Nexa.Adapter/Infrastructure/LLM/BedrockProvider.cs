
using Nexa.Adapter.Models;

namespace Nexa.Adapter.Infrastructure.LLM
{
    public class BedrockProvider : ILLMProvider
    {
        public Task<LlmAnalysisResponse> Analyze(List<LlmMessage> messages)
        {
            throw new NotImplementedException();
        }

        public Task<string> CompleteChat(List<LlmMessage> messages)
        {
            throw new NotImplementedException();
        }
    }

    public class AwsResponseParser : ILLMResponseParser
    {
        public NexaLlmResponse Parse(string rawResponse)
        {
            throw new NotImplementedException();
        }
    }
}
