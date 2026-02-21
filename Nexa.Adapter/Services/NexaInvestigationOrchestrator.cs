using Nexa.Adapter.Infrastructure.LLM;
using Nexa.Adapter.Models;

namespace Nexa.Adapter.Services
{
    public interface IInvestigationOrchestrator
    {
        public Task<InvestigationResponse> AnalyzeAsync(Alert request);
    }
    public class NexaInvestigationOrchestrator (IBankDataAggregator bankDataAggregator, ILLMProvider lLMProvider, IAnalyticalEngine analyticalEngine, IPromptBuilder promptBuilder): IInvestigationOrchestrator
    {
        private readonly IBankDataAggregator _aggregator= bankDataAggregator;
        private readonly IAnalyticalEngine _analyticalEngine=analyticalEngine;
        private readonly IPromptBuilder _promptBuilder = promptBuilder;
        private readonly ILLMProvider _llmClient= lLMProvider;
       // private readonly IInvestigationRepository _repository;

        public async Task<InvestigationResponse> AnalyzeAsync(Alert request)
        {
            // 1️⃣ Aggregate bank data
            var context = await _aggregator.BuildContextAsync(request);

            // 2️⃣ Run deterministic analytical logic
            var analyticalResult = _analyticalEngine.Analyze(context);

            // 3️⃣ Build prompt
            var prompt = _promptBuilder.BuildAnalyticalPrompt(context, analyticalResult);

            // 4️⃣ Call LLM
            var messages = new List<LlmMessage>
            {
                new LlmMessage { Role = Role.System, Content = prompt }
            };
            
            var llmResponse = await _llmClient.Analyze(messages);

            // 5️⃣ Merge structured + narrative output
            var response = MergeResults(analyticalResult, llmResponse);

            // 6️⃣ Persist investigation record
            // await _repository.SaveAsync(context, analyticalResult, prompt, llmResponse);

            return response;
        }

        private InvestigationResponse MergeResults(AnalyticalResult analyticalResult, LlmAnalysisResponse llm)
        {
            var response = new InvestigationResponse
            {
                InvestigationId = Guid.NewGuid(),
                AnalysisTimestamp = DateTime.UtcNow,

                // ✅ Deterministic truth (never overridden)
                Evidence = analyticalResult?.Evidence,
                FalsePositiveScore = analyticalResult?.FalsePositiveScore ?? 0,
                FalsePositiveLikelihood = analyticalResult?.FalsePositiveLikelihood ?? RiskLevel.Unknown,
                ConfidenceScore = analyticalResult?.ConfidenceScore ?? 0,

                // ✅ LLM enrichment (nullable safe)
                NarrativeSummary = llm?.NarrativeSummary ?? "Narrative unavailable",
                AlertRiskPosture = llm?.AlertRiskPosture ?? DeriveRiskPosture(analyticalResult),

                EvidenceMatrix = llm?.EvidenceMatrix ?? BuildFallbackEvidenceMatrix(analyticalResult),

                BehaviouralComparison = llm?.BehaviouralComparison ?? BuildFallbackBehaviourComparison(),

                Contradictions = llm?.Contradictions ?? new List<string>(),

                RecommendedAction = llm?.RecommendedAction ?? BuildFallbackAction(analyticalResult),

                ConfidenceJustification = llm?.Confidence?.Justification
                                          ?? BuildFallbackConfidenceStatement(analyticalResult),
                Raw=llm

            };

            // 🚨 Governance Shield — detect contradictions
            ValidateNarrativeConsistency(response, analyticalResult);

            return response;
        }
        private string DeriveRiskPosture(AnalyticalResult result)
        {
            if (result == null) return "Unknown";

            if (result.ConfidenceScore >= 80) return "High";
            if (result.ConfidenceScore >= 50) return "Moderate";

            return "Low";
        }

        private List<EvidenceMatrixItem> BuildFallbackEvidenceMatrix(AnalyticalResult result)
        {
            return new List<EvidenceMatrixItem>
                    {
                        new EvidenceMatrixItem
                        {
                            Signal = "Deterministic Engine Evaluation",
                            Observation = "Evidence generated using analytical scoring engines.",
                            RiskImpact = DeriveRiskImpact(result)
                        }
                    };
        }
        private void ValidateNarrativeConsistency(InvestigationResponse response,AnalyticalResult analytical)
        {
            if (analytical == null || response == null)
                return;

            // Example contradiction logic
            if (response.AlertRiskPosture == "High"
                && analytical.FalsePositiveLikelihood == RiskLevel.High)
            {
                response.Contradictions.Add(
                    "Narrative risk posture conflicts with deterministic false positive likelihood.");
            }
        }
        private BehaviouralComparison BuildFallbackBehaviourComparison()
        {
            return new BehaviouralComparison
            {
                AmountDeviation = "Behavioural deviation analysis unavailable due to narrative engine limitations.",
                ChannelConsistency = "Channel consistency assessment unavailable.",
                ActivityConsistency = "Activity consistency evaluation unavailable."
            };
        }

        private RecommendedAction BuildFallbackAction(AnalyticalResult analytical)
        {
            if (analytical == null)
            {
                return new RecommendedAction
                {
                    Action = "Review",
                    Rationale = "Automated recommendation unavailable. Analyst review required."
                };
            }

            if (analytical.ConfidenceScore >= 80)
            {
                return new RecommendedAction
                {
                    Action = "Escalate",
                    Rationale = "High analytical confidence with elevated risk indicators."
                };
            }

            if (analytical.FalsePositiveLikelihood == RiskLevel.High)
            {
                return new RecommendedAction
                {
                    Action = "Review",
                    Rationale = "High false positive likelihood requires analyst validation."
                };
            }

            return new RecommendedAction
            {
                Action = "Review",
                Rationale = "Default conservative action due to narrative engine unavailability."
            };
        }

        private string BuildFallbackConfidenceStatement(AnalyticalResult analytical)
        {
            if (analytical == null)
                return "Confidence assessment unavailable due to analytical evaluation failure.";

            if (analytical.ConfidenceScore >= 80)
                return "High confidence derived from deterministic analytical evidence scoring.";

            if (analytical.ConfidenceScore >= 50)
                return "Moderate confidence. Further analyst validation recommended.";

            return "Low confidence. Manual investigation required.";
        }

        private string DeriveRiskImpact(AnalyticalResult analytical)
        {
            if (analytical == null)
                return "Medium";

            if (analytical.ConfidenceScore >= 80)
                return "High";

            if (analytical.ConfidenceScore >= 50)
                return "Medium";

            return "Low";
        }

    }
}
