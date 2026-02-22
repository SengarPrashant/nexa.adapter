using Nexa.Adapter.Models;

namespace Nexa.Adapter.Services
{
    public interface IAnalyticalEngine
    {
        public AnalyticalResult Analyze(AlertInvestigationContext input);
    }
    public class AnalyticalEngine (IEvidenceWeightingEngine evidenceWeightingEngine, IFalsePositiveFramework falsePositiveFramework, IConfidenceScoreEngine confidenceScoreEngine) : IAnalyticalEngine
    {
        private readonly IEvidenceWeightingEngine _evidenceEngine= evidenceWeightingEngine;
        private readonly IFalsePositiveFramework _fpFramework = falsePositiveFramework;
        private readonly IConfidenceScoreEngine _confidenceEngine = confidenceScoreEngine;

        public AnalyticalResult Analyze(AlertInvestigationContext input)
        {
            var evidence = _evidenceEngine.Evaluate(input);

            var fpScore = _fpFramework.CalculateScore(input, evidence);
            var fpLikelihood = _fpFramework.DetermineLikelihood(fpScore);

            var confidence = _confidenceEngine.Calculate(input, evidence);

            return new AnalyticalResult
            {
                Evidence = evidence,
                FalsePositiveScore = fpScore,
                FalsePositiveLikelihood = fpLikelihood,
                ConfidenceScore = confidence
            };
        }
    }

    public enum EvidenceStrength { None = 0, Weak = 1, Moderate = 2, Strong = 3 }

    public enum RiskLevel
    {
        Low,
        Medium,
        High,
        Unknown
    }

    public class EvidenceResult
    {
        public EvidenceStrength TransactionPatternConsistency { get; set; }
        public EvidenceStrength HistoricalBehaviorAlignment { get; set; }
        public EvidenceStrength BeneficiaryRisk { get; set; }
        public EvidenceStrength VelocityAnomaly { get; set; }
        public EvidenceStrength EvaluateNewBeneficiary { get; set; }
    }


    public class AnalyticalResult
    {
        public EvidenceResult Evidence { get; set; }

        public double FalsePositiveScore { get; set; }
        public RiskLevel FalsePositiveLikelihood { get; set; }

        public double ConfidenceScore { get; set; }
    }

}
