using Nexa.Adapter.Models;

namespace Nexa.Adapter.Services
{
    public interface IConfidenceScoreEngine
    {
        public double Calculate(AlertInvestigationContext input, EvidenceResult evidence);
    }
    public class ConfidenceScoreEngine: IConfidenceScoreEngine
    {
        public double Calculate(AlertInvestigationContext input, EvidenceResult evidence)
        {
            double score = 1.0;

            if (input.Transaction == null || !input.TransactionHistory.Any())
                score -= 0.3;

            if (input.AlertHistory == null)
                score -= 0.1;

            if (input.CustomerBehaviour == null)
                score -= 0.2;

            return Math.Max(score, 0.0);
        }
    }

}
