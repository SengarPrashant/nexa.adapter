
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
         //   var credentials = new SessionAWSCredentials(
         //   Environment.GetEnvironmentVariable("ASIA6ICX642MNRBEVUUP"),
         //   Environment.GetEnvironmentVariable("V+CyQnVzE/2cbcuhIFtzgChNextpA6Nl+l2Wvwx4"),
         //   Environment.GetEnvironmentVariable("IQoJb3JpZ2luX2VjEBIaCXVzLWVhc3QtMiJGMEQCIELOJBtZfmQ+eBs/noH3qPLNSlQDfMepOjPL1ou57XteAiBYMVibHq6zLhIEj7jTrzm01dIynHetKQzRmpjJ/M526yrMAwjb//////////8BEAAaDDk3OTQzNzAyMDgyNCIMC1yuL5vmS8139e/lKqAD3GIUSmijIjp+ovzHbu+QeOdknE1Cw5oUvwMUwobUpZzS52bLAbI+F5aUIgCRKdi8VCiNXs+kDCknhQe84Ug8Q3HIIkJ0ZLJY06ubOuq0qubYA2mlk6K3d6cRUPBztRrvRaXJ+yPsDISFkPt39ocdYn9cdJy8ndpUztQdnHeo9txSqYlFOH2dQGNLkvHoXNQB4MUG/e1sZ/ML8mEoGhhQM8L/JZeMtJn1VvA4Nc12B9Vm3klVb43j1d4wvy30QZ3pxhoKQhilINJY5UIqp3ldxCwnyuP00Pkn3VLY7w1WlVg0QM7s5C1InXXUK1MwUA/PC/WGVQdTXkABJjD6V2fgBX7HiR4Cl0gX7ni8mgfc9y6KOamJ8YpWG0V8KhNJYaKAyXhwAHWfP5W18hqpqZpNjiCgWHP2gvtZkwb+IokTNbXkJXMit5knEbFvamtcW1KmJFA7WFMk+JY3HRc0EME4Gd/f9+xtsRpdyQdht8ndjV3aSWU6M/4BvRqU8N0ZWAS9FMBO6bbwnN9GxlLDthJCTeJBe2twIS7QbYwfo1DDGbsw1NDwzAY6pQHTm90GOT2jMVc4tAoz+816pO1TKIxKKargvyVlE6w9r6BaPMq524tiorPD50wWDutLOEiOyJjp9Xgua83CK7gp3T5NfHdD6Md+R5ImwgJWde3hxEHISMn0zriHegG08Pe+P6hqOKqZ03nF57W8W5rw2wX7SoVWoU8gZRAkAz7kaPH5ECKS1UMJYjphZS0oN1bC6qViWphfi+rHlXiDTmAWA8FNl5Q=")
         //);
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
