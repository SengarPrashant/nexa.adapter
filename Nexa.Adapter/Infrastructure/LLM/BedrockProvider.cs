
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
using Nexa.Adapter.Models;
using System.Text;
using System.Text.Json;

namespace Nexa.Adapter.Infrastructure.LLM
{
    public class BedrockProvider : ILLMProvider
    {
        private readonly AmazonBedrockRuntimeClient _client;
        private readonly string _modelId;
        private readonly ILLMResponseParser _parser;
        public BedrockProvider(IConfiguration config, ILLMResponseParser parser)
        {
            _modelId = config["LLM:Bedrock:ModelId"];
            var credentials = new SessionAWSCredentials(
            Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
            Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"),
            Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN")
         );
            _client = new AmazonBedrockRuntimeClient(
                credentials,
                Amazon.RegionEndpoint.USWest2
            );
            _parser = parser;
        }
        private async Task<string> InvokeLLM(List<LlmMessage> messages)
        {
            var messagList = new List<object>();

            var filteredMessages = messages.Where(x => x.Role != Role.System).ToList();
            foreach (var item in filteredMessages)
            {
                messagList.Add(new
                {
                    role = item.Role.ToString().ToLower(),
                    content = new[]
                    {
                        new { text = item.Content}
                    }
                });
            }
            if (!messagList.Any())
            {
                messagList.Add(new
                {
                    role = "user",
                    content = new[]
                    {
                        new { text = "please follow the given instructions in the system prompt and provide output in given format"}
                    }
                });
            }
            var requestBody = new
            {
                system = new[] { new { text = messages.Where(x => x.Role == Role.System).FirstOrDefault().Content } },
                messages = messagList,
                inferenceConfig = new
                {
                    maxTokens = 1000,
                    temperature = 0.7,
                    topP = 0.9
                }
            };

            var request = new InvokeModelRequest
            {
                ModelId = _modelId,
                ContentType = "application/json",
                Accept = "application/json",
                Body = new MemoryStream(
                    Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(requestBody)))
            };

            var response = await _client.InvokeModelAsync(request);

            using var reader = new StreamReader(response.Body);
            var llmResult = await reader.ReadToEndAsync();
            return llmResult;
            
        }
        public async Task<LlmAnalysisResponse>  Analyze(List<LlmMessage> messages)
        {
            string llmResponse= await InvokeLLM(messages);
            var obj= _parser.Parse<LlmAnalysisResponse>(llmResponse);
            return obj;
        }
        
        public async Task<string> CompleteChat(List<LlmMessage> messages)
        {
            string llmResponse = await InvokeLLM(messages);
            return llmResponse;
        }
    }

    public class AwsResponseParser : ILLMResponseParser
    {
        public T Parse<T>(string rawResponse)
        {
            var jsonString = ExtractText(rawResponse);
            string cleanedJson = jsonString
            .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
            .Replace("```", "")
            .Trim();
            var obj = JsonConvert.DeserializeObject<T>(cleanedJson);
            return obj;
        }
        private static string ExtractText(string json)
        {
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("output")
                .GetProperty("message")
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString()!;
        }
    }
}
