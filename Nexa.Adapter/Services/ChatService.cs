using Microsoft.Extensions.Caching.Memory;
using Nexa.Adapter.Infrastructure.LLM;
using Nexa.Adapter.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nexa.Adapter.Services
{
    public interface IChatService
    {
        Task<NexaLlmResponse> ProcessChat(ChatRequest chat);
    }

    public class ChatService : IChatService
    {
        private readonly IMemoryCache _cache;
        private readonly ILLMProvider _llmProvider;
        private readonly ILLMResponseParser _llmParser;
        private readonly IPromptBuilder _promptBuilder;
        private readonly IEnumerable<ITool> _tools;

        public ChatService(IMemoryCache memoryCache, ILLMProvider llmProvider, ILLMResponseParser responseParser, IPromptBuilder promptBuilder, IEnumerable<ITool> tools)
        {
            _cache = memoryCache;
            _llmProvider = llmProvider;
            _llmParser = responseParser;
            _promptBuilder = promptBuilder;
            _tools = tools ?? new List<ITool>();
        }

        public async Task<NexaLlmResponse> ProcessChat(ChatRequest chat)
        {
            try
            {
                var historyFound = string.IsNullOrEmpty(chat.SessionId) ? false : _cache.TryGetValue<List<LlmMessage>>(chat.SessionId.Trim(), out var result);
                var message = new List<LlmMessage>();
                if (!historyFound)
                {
                    var systemPrompt = _promptBuilder.BuildFollowUpPrompt(chat.InitialContext);
                    message.Add(new LlmMessage { Role = Role.System, Content = systemPrompt });
                }
                message.Add(new LlmMessage { Role = Role.User, Content = chat.Content });

                // Step 1: Add tools metadata
                            if (_tools.Any())
                {
                    var toolDescriptions = _tools.Select(t => new { t.Name, t.Description }).ToList();
                    var toolsJson = JsonSerializer.Serialize(toolDescriptions);
                    var toolsMetadata = $"AVAILABLE_TOOLS:\n<TOOLS_JSON>\n{toolsJson}\n</TOOLS_JSON>";
                    message.Add(new LlmMessage { Role = Role.User  , Content = toolsMetadata });
                }

                // Step 2: LLM Analyze
                var analysis = await _llmProvider.Analyze(message);

                // Step 3: Execute tools if suggested
                List<ToolResult> toolResults = new List<ToolResult>();
                if (analysis?.ToolsToCall != null && analysis.ToolsToCall.Any())
                {
                    foreach (var toolCall in analysis.ToolsToCall)
                    {
                        var tool = _tools.FirstOrDefault(t => string.Equals(t.Name, toolCall.ToolName, System.StringComparison.OrdinalIgnoreCase));
                        if (tool != null)
                        {
                            var result1 = await tool.ExecuteAsync(toolCall);
                            toolResults.Add(result1);
                        }
                        else
                        {
                            toolResults.Add(new ToolResult { ToolName = toolCall.ToolName, Output = "Tool not found" });
                        }
                    }
                }

                // Step 4: Send tool results back to LLM for final response
                var followUpMessages = new List<LlmMessage>();
                if (!historyFound)
                {
                    var systemPrompt = _promptBuilder.BuildFollowUpPrompt(chat.InitialContext);
                    followUpMessages.Add(new LlmMessage { Role = Role.System, Content = systemPrompt });
                }
                followUpMessages.Add(new LlmMessage { Role = Role.User, Content = chat.Content });
                followUpMessages.Add(new LlmMessage { Role = Role.Assistant, Content = JsonSerializer.Serialize(analysis) });
                if (toolResults.Any())
                {
                    followUpMessages.Add(new LlmMessage { Role = Role.User, Content = $"Tool results:\n{JsonSerializer.Serialize(toolResults)}" });
                }

                var finalResponse = await _llmProvider.CompleteChat(followUpMessages);
                var nexaResponse = _llmParser.Parse<NexaLlmResponse>(finalResponse);
                nexaResponse.SessionId = string.IsNullOrEmpty(chat.SessionId) ? System.Guid.NewGuid().ToString() : chat.SessionId.Trim();
                return nexaResponse;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"[NEXA] ProcessChat error: {ex}");
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
