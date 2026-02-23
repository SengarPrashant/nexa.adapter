namespace Nexa.Adapter.Models
{
    public class ChatRequest
    {
        public string? SessionId { get; set; }
        public string Content { get; set; }
        public InvestigationResponse? InitialContext { get; set; }
    }

    public class NexaLlmResponse
    {
        public string? SessionId { get; set; }
        public string ResponseType { get; set; }
        public string Response { get; set; }
        public List<string> EvidenceReference { get; set; }
        public string ConfidenceStatement { get; set; }
    }
}
