using Nexa.Adapter.Models;

namespace Nexa.Adapter.Services
{
    public interface IPromptBuilder
    {
        string BuildAnalyticalPrompt(AlertInvestigationContext context, AnalyticalResult analysis);
        string BuildFollowUpPrompt(InvestigationResponse investigation);
    }
    public class DefaultPromptBuilder: IPromptBuilder
    {
        public string BuildAnalyticalPrompt(AlertInvestigationContext context, AnalyticalResult analysis)
        {
            var e = analysis.Evidence;
            return $@"
                    SYSTEM ROLE:
                    You are a senior banking risk analyst assisting investigation of transaction alerts.

                    STRICT RULES:
                    - DO NOT invent facts
                    - USE ONLY provided data
                    - If data insufficient → explicitly state uncertainty
                    - Use professional compliance / risk language
                    - Output MUST be valid JSON
                    - DO NOT include explanations outside JSON
                    - DO NOT include markdown
                    - DO NOT include commentary

                    TASK:
                    Assess the transaction alert and produce structured analytical response.

                    TRANSACTION DETAILS:
                    - Transaction Id: {context.Transaction.TransactionId}
                    - Type: {context.Transaction.Type}
                    - Amount: {context.Transaction.Amount} {context.Transaction.Currency}
                    - Channel: {context.Transaction.Channel}
                    - Timestamp: {context.Transaction.Timestamp}
                    - Source Account: {context.Transaction.SourceAccount}
                    - Location: {context.Transaction.Geolocation?.City}, {context.Transaction.Geolocation?.Country}

                    CUSTOMER PROFILE:
                    - Customer Id: {context.CustomerProfile.CustomerId}
                    - Risk Rating: {context.CustomerProfile.RiskRating}
                    - Segment: {context.CustomerProfile.Segment}
                    - KYC Level: {context.CustomerProfile.KycLevel}

                    CUSTOMER BEHAVIOUR BASELINE:
                    - Average Transaction Amount: {context.CustomerBehaviour.AvgTransactionAmount}
                    - Maximum Historical Amount: {context.CustomerBehaviour.MaxTransactionAmount}
                    - Preferred Channels: {string.Join(", ", context.CustomerBehaviour.PreferredChannels ?? new List<string>())}
                    - Last Activity Timestamp: {context.CustomerBehaviour.LastActivityTimeStamp}

                    ALERT CONTEXT:
                    - Alert Code: {context.Alert.AlertCode}
                    - Alert Source: {context.Alert.AlertSource}
                    - Severity: {context.Alert.Severity}
                    - Risk Score: {context.Alert.RiskScore}

                    ANALYTICAL SIGNALS:
                        Evidence Strength:
                        - Transaction Pattern Consistency: {e.TransactionPatternConsistency}
                        - Historical Behavior Alignment: {e.HistoricalBehaviorAlignment}
                        - Beneficiary Risk: {e.BeneficiaryRisk}
                        - Velocity Anomaly: {e.VelocityAnomaly}
                        - New Beneficiary Evaluation: {e.EvaluateNewBeneficiary}

                        False Positive Analysis:
                        - Score: {analysis.FalsePositiveScore}
                        - Likelihood: {analysis.FalsePositiveLikelihood}

                        Confidence Score:
                        - {analysis.ConfidenceScore}

                    OUTPUT FORMAT (MANDATORY JSON):
                         {{
                          ""narrativeSummary"": ""string"",
                          ""alertRiskPosture"": ""Low | Moderate | High"",

                          ""evidenceMatrix"": [
                            {{
                              ""signal"": ""string"",
                              ""observation"": ""string"",
                              ""riskImpact"": ""Low | Medium | High""
                            }}
                          ],

                          ""behaviouralComparison"": {{
                            ""amountDeviation"": ""string"",
                            ""channelConsistency"": ""string"",
                            ""activityConsistency"": ""string""
                          }},

                          ""contradictions"": [""string""],

                          ""recommendedAction"": {{
                            ""action"": ""Review | Escalate | CustomerContact | Close"",
                            ""rationale"": ""string""
                          }},

                          ""confidence"": {{
                            ""score"": 0.0,
                            ""justification"": ""string""
                          }}
                        }}

                    ";
        }
        public string BuildFollowUpPrompt(InvestigationResponse investigation)
        {
            return $@"
                    SYSTEM ROLE:
                    You are a senior banking risk analyst assistant operating in regulated investigation mode. 

                    STRICT RULES:
                    - DO NOT invent facts
                    - USE ONLY provided investigation data
                    - DO NOT modify deterministic scores
                    - If uncertain, explicitly state limitations
                    - Provide compliance-grade reasoning
                    - Avoid conversational tone
                    - Responses must remain audit-safe

                    INVESTIGATION CONTEXT:
                    - InvestigationId: {investigation.InvestigationId}
                    - AnalysisTimestamp: {investigation.AnalysisTimestamp}

                    DETERMINISTIC ANALYTICAL RESULTS:
                    - False Positive Score: {investigation.FalsePositiveScore}
                    - False Positive Likelihood: {investigation.FalsePositiveLikelihood}
                    - Confidence Score: {investigation.ConfidenceScore}

                    EVIDENCE SIGNALS:
                    - Transaction Pattern Consistency: {investigation.Evidence?.TransactionPatternConsistency}
                    - Historical Behavior Alignment: {investigation.Evidence?.HistoricalBehaviorAlignment}
                    - Beneficiary Risk: {investigation.Evidence?.BeneficiaryRisk}
                    - Velocity Anomaly: {investigation.Evidence?.VelocityAnomaly}
                    - New Beneficiary Indicator: {investigation.Evidence?.EvaluateNewBeneficiary}

                    PRIOR ANALYTICAL NARRATIVE:
                    Summary:
                    {investigation.NarrativeSummary}

                    BEHAVIOURAL COMPARISON:
                    - Amount Deviation: {investigation.BehaviouralComparison?.AmountDeviation}
                    - Channel Consistency: {investigation.BehaviouralComparison?.ChannelConsistency}
                    - Activity Consistency: {investigation.BehaviouralComparison?.ActivityConsistency}

                    KNOWN CONTRADICTIONS:
                    {FormatContradictions(investigation.Contradictions)}

                    RECOMMENDED ACTION:
                    - Action: {investigation.RecommendedAction?.Action}
                    - Rationale: {investigation.RecommendedAction?.Rationale}

                    TASK:
                    Provide a regulatory-safe analytical response addressing the analyst question.
                    
                    ADDITIONAL INSTRUCTIONS:
                    If you think you should use any available tool, please use.

                    OUTPUT FORMAT (MANDATORY JSON):
                    {{
                      ""responseType"": ""Clarification | RiskAssessment | EvidenceExpansion | LimitationStatement | CaseNoteGenerated | General"",
                      ""response"": ""string"",
                      ""evidenceReference"": [""string""],
                      ""confidenceStatement"": ""string""
                    }}
                    ";
        }
        private string FormatContradictions(List<string> contradictions)
        {
            if (contradictions == null || contradictions.Count == 0)
                return "None";

            return string.Join("\n- ", contradictions);
        }

    }
}
