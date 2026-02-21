using Nexa.Adapter.Models;

namespace Nexa.Adapter.Services
{
    public interface IFalsePositiveFramework
    {
        public double CalculateScore(AlertInvestigationContext input, EvidenceResult evidence);
        public RiskLevel DetermineLikelihood(double score);
    }
    public class FalsePositiveFramework: IFalsePositiveFramework
    {
        public double CalculateScore(AlertInvestigationContext input, EvidenceResult evidence)
        {
            // Weighted scoring model
            double score = 0;

            score += (int)evidence.TransactionPatternConsistency * 0.35;
            score += (int)evidence.HistoricalBehaviorAlignment * 0.30;
            score += (int)evidence.BeneficiaryRisk * 0.20;
            score += (int)evidence.VelocityAnomaly * 0.15;

            // Normalize (max = 3)
            return score / 3.0;
        }

        public RiskLevel DetermineLikelihood(double score)
        {
            if (score >= 0.7)
                return RiskLevel.Low;      // Low risk → Likely False Positive

            if (score >= 0.4)
                return RiskLevel.Medium;

            return RiskLevel.High;
        }
    }

}
