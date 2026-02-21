using Nexa.Adapter.Services;

namespace Nexa.Adapter.Models
{
    public class LlmAnalysisResponse
    {
        public string NarrativeSummary { get; set; }

        public string AlertRiskPosture { get; set; }

        public List<EvidenceMatrixItem> EvidenceMatrix { get; set; }

        public BehaviouralComparison BehaviouralComparison { get; set; }

        public List<string> Contradictions { get; set; }

        public RecommendedAction RecommendedAction { get; set; }

        public ConfidenceBlock Confidence { get; set; }
    }

    public class EvidenceMatrixItem
    {
        public string Signal { get; set; }
        public string Observation { get; set; }
        public string RiskImpact { get; set; }
    }

    public class BehaviouralComparison
    {
        public string AmountDeviation { get; set; }
        public string ChannelConsistency { get; set; }
        public string ActivityConsistency { get; set; }
    }

    public class RecommendedAction
    {
        public string Action { get; set; }
        public string Rationale { get; set; }
    }

    public class ConfidenceBlock
    {
        public double Score { get; set; }
        public string Justification { get; set; }
    }

    public class InvestigationResponse
    {
        public Guid InvestigationId { get; set; }
        public DateTime AnalysisTimestamp { get; set; }

        // Deterministic truth
        public EvidenceResult Evidence { get; set; }
        public double FalsePositiveScore { get; set; }
        public RiskLevel FalsePositiveLikelihood { get; set; }
        public double ConfidenceScore { get; set; }

        // LLM enrichment
        public string NarrativeSummary { get; set; }
        public string AlertRiskPosture { get; set; }
        public List<EvidenceMatrixItem> EvidenceMatrix { get; set; }
        public BehaviouralComparison BehaviouralComparison { get; set; }
        public List<string> Contradictions { get; set; }
        public RecommendedAction RecommendedAction { get; set; }

        // Governance
        public string ConfidenceJustification { get; set; }
    }


}
