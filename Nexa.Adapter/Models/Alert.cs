using System.Text.Json.Serialization;

namespace Nexa.Adapter.Models
{
    public class Alert
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        public string? AlertCode { get; set; }
        public bool IsVelocityAnomaly { get; set; }
        public string? AlertType { get; set; }

        public string? AlertSource { get; set; }

        public string? Severity { get; set; }

        public int CustomerId { get; set; }

        public string? CustomerName { get; set; }

        public string? AccountNo { get; set; }

        public decimal Amount { get; set; }

        public string? Currency { get; set; }

        public string? Description { get; set; }

        public DateTime? AlertTimeStamp { get; set; }

        public string? Status { get; set; }

        public int? RiskScore { get; set; }

        public string? AssignedTo { get; set; }

        public string? ResolutionReason { get; set; }

        public DateTime? AlertResolutionTimeStamp { get; set; }
    }
}
