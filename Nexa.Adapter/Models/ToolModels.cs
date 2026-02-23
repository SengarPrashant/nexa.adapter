namespace Nexa.Adapter.Models
{
    public class ToolCall
    {
        public string ToolName { get; set; } = string.Empty;
        public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();
    }

    public class ToolResult
    {
        public string ToolName { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
    }
}