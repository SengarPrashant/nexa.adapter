namespace Nexa.Adapter.Models
{
    /// <summary>
    /// Canonical investigation object passed into Narrative / AI Analysis Engine
    /// </summary>
    public class AlertInvestigationContext
    {
        /// <summary>
        /// Primary alert being analysed
        /// </summary>
        public Alert Alert { get; set; }

        public List<Alert> AlertHistory { get; set; }


        /// <summary>
        /// Triggering transaction (most important)
        /// </summary>
        public Transaction Transaction { get; set; }

        /// <summary>
        /// Behavioural baseline intelligence (CRITICAL FOR AI)
        /// </summary>
        public CustomerBehaviourProfile CustomerBehaviour { get; set; }

        /// <summary>
        /// Customer behavioural / risk profile
        /// </summary>
        public CustomerProfile CustomerProfile { get; set; }

        /// <summary>
        /// Optional historical transactions used for comparison
        /// </summary>
        public List<Transaction> TransactionHistory { get; set; }

        /// <summary>
        /// Optional cross-alert intelligence
        /// </summary>
        public List<Alert> RelatedAlerts { get; set; }

        /// <summary>
        /// Optional external risk signals
        /// (sanctions hits, device risk, geo risk, etc.)
        /// </summary>
        public Dictionary<string, object> ExternalSignals { get; set; }
    }
}
