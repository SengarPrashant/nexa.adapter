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
}