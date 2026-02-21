namespace Nexa.Adapter.Models
{
    public class CustomerBehaviourProfile
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int AvgNoOfMonthlyTransaction { get; set; }
        public decimal AvgTransactionAmount { get; set; }

        public decimal MaxTransactionAmount { get; set; }

        public List<string> PreferredChannels { get; set; }

        public List<string> FrequentMerchants { get; set; }

        public string TransactionPattern { get; set; }

        public int LoginFrequency { get; set; }

        public DateTime LastActivityTimeStamp { get; set; }
    }
}
