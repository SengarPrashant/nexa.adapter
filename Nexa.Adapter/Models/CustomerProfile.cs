namespace Nexa.Adapter.Models
{
    public class CustomerProfile
    {
        public int CustomerId { get; set; }

        public string KycLevel { get; set; }

        public string Occupation { get; set; }

        public string RelationshipType { get; set; }

        public RiskRating RiskRating { get; set; }

        public string Segment { get; set; }

        public List<string> LinkedAccounts { get; set; }
    }
    public enum RiskRating
    {
        Low,
        Medium,
        High
    }
}
