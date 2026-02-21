namespace Nexa.Adapter.Models
{
    public class ChatRequest
    {
        public string? SessionId { get; set; }
        public string Content { get; set; }
        public LlmAnalysisResponse? InitialContext { get; set; }
    }
}
