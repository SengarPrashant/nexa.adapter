using Nexa.Adapter.Models;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nexa.Adapter.Services
{
    public class AccountLookupTool : ITool
    {
        public string Name => "AccountLookup";
        public string Description => "Lookup account metadata such as customer name, risk rating and account open date.";

        public object InputSchema => new
        {
            type = "object",
            properties = new
            {
                accountId = new
                {
                    type = "string",
                    description = "Account identifier"
                }
            },
            required = new[] { "accountId" }
        };

        public Task<ToolResult> ExecuteAsync(ToolCall call)
        {
            var accountId = call?.Args?.ContainsKey("accountId") == true ? call.Args["accountId"] : "unknown";
            var outputObj = new { accountId, customerName = "ACME Corp", riskRating = "Low", opened = "2018-05-01" };
            var output = JsonSerializer.Serialize(outputObj);
            return Task.FromResult(new ToolResult { ToolName = Name, Output = output });
        }
    }

    public class TransactionSearchTool : ITool
    {
        public string Name => "TransactionSearch";
        public string Description => "Search recent transactions for an account and return aggregated statistics.";

        public object InputSchema => new
        {
            type = "object",
            properties = new
            {
                accountId = new
                {
                    type = "string",
                    description = "Account identifier"
                }
            },
            required = new[] { "accountId" }
        };

        public Task<ToolResult> ExecuteAsync(ToolCall call)
        {
            var accountId = call?.Args?.ContainsKey("accountId") == true ? call.Args["accountId"] : "unknown";
            var outputObj = new { accountId, transactionCount = 5, totalAmount = 1250.50 };
            var output = JsonSerializer.Serialize(outputObj);
            return Task.FromResult(new ToolResult { ToolName = Name, Output = output });
        }
    }

    public class WeatherTool : ITool
    {
        public string Name => "Weather";
        public string Description => "Fetch current weather for a location. Args: { \"location\": \"London\" }";

        public object InputSchema => new
        {
            type = "object",
            properties = new
            {
                location = new
                {
                    type = "string",
                    description = "City name"
                }
            },
            required = new[] { "location" }
        };

        public Task<ToolResult> ExecuteAsync(ToolCall call)
        {
            var location = call?.Args?.ContainsKey("location") == true ? call.Args["location"] : "Unknown";
            var seed = (location ?? "").GetHashCode();
            var rng = new Random(seed);
            var temp = Math.Round(-5 + rng.NextDouble() * 35, 1);
            var conditions = new[] { "sunny", "cloudy", "rain", "snow" };
            var condition = conditions[Math.Abs(seed) % conditions.Length];
            var humidity = rng.Next(20, 100);
            var wind = Math.Round(rng.NextDouble() * 12, 1);
            var outputObj = new { location, temperatureCelsius = temp, condition, humidityPercent = humidity, windSpeedMetersPerSecond = wind };
            var output = JsonSerializer.Serialize(outputObj);
            return Task.FromResult(new ToolResult { ToolName = Name, Output = output });
        }
    }

    public class FetchUrlContentTool : ITool
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public string Name => "FetchUrlContent";

        public object InputSchema => new
        {
            type = "object",
            properties = new
            {
                url = new
                {
                    type = "string",
                    description = "Full HTTPS URL to fetch data from"
                }
            },
            required = new[] { "url" }
        };

        public string Description =>
            "Fetch data from a public URL using HTTP GET. " +
            "Use this tool whenever the user provides a URL and asks to fetch, call, retrieve, or get data from it. " +
            "Required args: { \"url\": \"full https url\" }. " +
            "Example: user says 'fetch data from https://example.com/api'.";

        public async Task<ToolResult> ExecuteAsync(ToolCall call)
        {
            try
            {
                var url = call?.Args?.ContainsKey("url") == true
                    ? call.Args["url"]
                    : null;

                if (string.IsNullOrWhiteSpace(url))
                    return Error("URL is required.");

                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    return Error("Invalid URL.");

                var response = await _httpClient.GetAsync(uri);
                var content = await response.Content.ReadAsStringAsync();

                return new ToolResult
                {
                    ToolName = Name,
                    Output = JsonSerializer.Serialize(new
                    {
                        success = response.IsSuccessStatusCode,
                        statusCode = (int)response.StatusCode,
                        url,
                        data = TryParseJson(content)
                    })
                };
            }
            catch (Exception ex)
            {
                return Error(ex.Message);
            }
        }

        private static object TryParseJson(string content)
        {
            try { return JsonSerializer.Deserialize<object>(content); }
            catch { return content; }
        }

        private static ToolResult Error(string message)
        {
            return new ToolResult
            {
                ToolName = "FetchUrlContent",
                Output = JsonSerializer.Serialize(new { success = false, error = message })
            };
        }
    }
}