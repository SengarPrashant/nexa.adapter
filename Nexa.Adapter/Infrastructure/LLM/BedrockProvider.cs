
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
using Nexa.Adapter.Models;
using Nexa.Adapter.Services;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static Nexa.Adapter.Infrastructure.LLM.AwsResponseParser;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Nexa.Adapter.Infrastructure.LLM
{
    public class BedrockProvider : ILLMProvider
    {
        private readonly AmazonBedrockRuntimeClient _client;
        private readonly string _modelId;
        private readonly ILLMResponseParser _parser;
        private readonly IEnumerable<ITool> _tools;
        public BedrockProvider(IConfiguration config, ILLMResponseParser parser, IEnumerable<ITool> tools)
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
            _tools= tools ?? new List<ITool>();
        }
        private async Task<string> InvokeLLM(List<LlmMessage> messages, IEnumerable<ITool>? toolSpecs = null)
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
            List<Object> lstTools= new List<Object>();
            if (toolSpecs != null && toolSpecs.Any())
            {
                foreach (var item in toolSpecs)
                {
                    lstTools.Add(new
                    {
                        toolSpec = new
                        {
                            name = item.Name,
                            description = item.Description,
                            inputSchema = new { json = item.InputSchema }
                        }
                    });
                }
            }
            var toolConfig = new
            {
                tools = lstTools
            };
            Object requestBody;
            if (toolConfig.tools.Any())
            {
                requestBody = new
                {
                    system = new[] { new { text = messages.Where(x => x.Role == Role.System).FirstOrDefault().Content } },
                    messages = messagList,
                    toolConfig = toolConfig,
                    inferenceConfig = new
                    {
                        maxTokens = 1000,
                        temperature = 0.7,
                        topP = 0.9
                    }
                };
            }
            else
            {
                 requestBody = new
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
            }
                

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
        
        public async Task<NexaLlmResponse> CompleteChat(List<LlmMessage> messages, IEnumerable<ITool>? toolSpecs = null)
        {
            string llmResponse = await InvokeLLM(messages, toolSpecs);
            NexaLlmResponse obj= new NexaLlmResponse();
            var (toolUse, text) = _parser.ExtractToolOrText(llmResponse);
            if (toolUse != null && toolUse.Any())
            {
                List<ToolResult> toolResults = new List<ToolResult>();
                    foreach (var toolCall in toolUse)
                    {
                        var tool = _tools.FirstOrDefault(t => string.Equals(t.Name, toolCall.Name, System.StringComparison.OrdinalIgnoreCase));
                        if (tool != null)
                        {
                         var inputList= JsonConvert.DeserializeObject<Dictionary<string, string>>(toolCall.Input);
                        var result1 = await tool.ExecuteAsync(new ToolCall() { ToolName= toolCall.Name, Args= inputList });
                            toolResults.Add(result1);
                        }
                        else
                        {
                            toolResults.Add(new ToolResult { ToolName = toolCall.Name, Output = "Tool not found" });
                        }
                    }
                messages.Add(new LlmMessage { Role = Role.Assistant, Content = JsonSerializer.Serialize(toolUse) });
                if (toolResults.Any())
                {
                    messages.Add(new LlmMessage { Role = Role.Assistant, Content = $"This is tool execution result, Please respond as per user query. Tool results:\n{JsonSerializer.Serialize(toolResults)}" });
                }
                var resultAfterToolCall = await InvokeLLM(messages);
             var (tools, textResult) = _parser.ExtractToolOrText(resultAfterToolCall);
             string cleanedJson = textResult
            .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
            .Replace("```", "")
            .Trim();
                obj = JsonConvert.DeserializeObject<NexaLlmResponse>(cleanedJson);

            }
            else
            {
             string cleanedJson = text
            .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
            .Replace("```", "")
            .Trim();
                cleanedJson = _parser.PrependThinkingToResponse(cleanedJson);
             obj = JsonConvert.DeserializeObject<NexaLlmResponse>(cleanedJson);
            }
            return obj;
        }
    }

    public class AwsResponseParser : ILLMResponseParser
    {
        private static readonly Regex ThinkingRegex =
        new Regex(@"<thinking>(.*?)</thinking>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public string PrependThinkingToResponse(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return rawText;

            // 1. Extract <thinking> block
            var thinkingMatch = ThinkingRegex.Match(rawText);
            string thinkingContent = string.Empty;

            if (thinkingMatch.Success)
            {
                thinkingContent = thinkingMatch.Groups[1].Value.Trim();
                rawText = ThinkingRegex.Replace(rawText, "").Trim();
            }

            // 2. Parse remaining JSON
            var jsonNode = JsonNode.Parse(rawText) as JsonObject;
            if (jsonNode == null || !jsonNode.ContainsKey("response"))
                return rawText;

            // 3. Prepend thinking to response
            var originalResponse = jsonNode["response"]?.GetValue<string>() ?? string.Empty;

            if (!string.IsNullOrEmpty(thinkingContent))
            {
                jsonNode["response"] =
                    $"[Analyst Reasoning]\n{thinkingContent}\n\n[Conclusion]\n{originalResponse}";
            }

            // 4. Return updated JSON
            return jsonNode.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        public T Parse<T>(string rawResponse, string? extractMethod = "ExtractText")
        {
            //var jsonString = extractMethod== "ExtractToolCall" ? ExtractToolCall(rawResponse) : ExtractText(rawResponse);
            var jsonString = ExtractToolOrText(rawResponse);
            var (toolUse, text) = ExtractToolOrText(rawResponse);
             string cleanedJson = text
            .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
            .Replace("```", "")
            .Trim();
            var obj = JsonConvert.DeserializeObject<T>(cleanedJson);
            return obj;
        }
        //public static string ExtractToolOrText(string json)
        //{
        //    string extractedResponse = "";
        //    var root = JsonNode.Parse(json);

        //    var contentArray = root?["output"]?["message"]?["content"]?.AsArray();
        //    if (contentArray == null || contentArray.Count == 0)
        //        return "";

        //    foreach (var item in contentArray)
        //    {
        //        // Case 1: Tool invocation
        //        if (item?["toolUse"] != null)
        //        {
        //            extractedResponse = item["toolUse"].GetValue<string>()!;
        //            //return (
        //            //    new ToolUse
        //            //    {
        //            //        Name = tool["name"]!.GetValue<string>(),
        //            //        ToolUseId = tool["toolUseId"]!.GetValue<string>(),
        //            //        Input = tool["input"]!.ToJsonString()
        //            //    },
        //            //    null
        //            //);
        //        }

        //        // Case 2: Plain text response
        //        if (item?["text"] != null)
        //        {
        //            extractedResponse= item["text"]!.GetValue<string>();
        //        }
        //    }

        //    return extractedResponse;
        //}
        public class ToolUse
        {
            public string Name { get; set; } = string.Empty;
            public string ToolUseId { get; set; } = string.Empty;

            // Keep input flexible (tools differ)
            public string Input { get; set; } = string.Empty;
        }

        public (List<ToolUse>? toolUse, string? text) ExtractToolOrText(string json)
        {
            List<ToolUse> lstTools = new List<ToolUse>();
            string text = "";
            var root = JsonNode.Parse(json);

            var contentArray = root?["output"]?["message"]?["content"]?.AsArray();
            if (contentArray == null || contentArray.Count == 0)
                return (lstTools, text);

            foreach (var item in contentArray)
            {
                // Case 1: Tool invocation
                if (item?["toolUse"] != null)
                {
                    var tool = item["toolUse"]!;
                    lstTools.Add(
                        new ToolUse
                        {
                            Name = tool["name"]!.GetValue<string>(),
                            ToolUseId = tool["toolUseId"]!.GetValue<string>(),
                            Input = tool["input"]!.ToJsonString()
                        });
                }

                // Case 2: Plain text response
                if (item?["text"] != null)
                {
                    text= item["text"]!.GetValue<string>();
                }
            }

            return (lstTools, text);
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
        public string ExtractToolCall(string json)
        {
            var responseObj = JsonConvert.DeserializeObject<object>(json);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("output")
                .GetProperty("message")
                .GetProperty("content")[0]
                .GetProperty("toolUse")
                .GetString()!;
        }
    }
}
