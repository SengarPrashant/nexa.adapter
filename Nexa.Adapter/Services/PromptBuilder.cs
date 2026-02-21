using Nexa.Adapter.Models;

namespace Nexa.Adapter.Services
{
    public interface IPromptBuilder
    {
        string Build(AlertInvestigationContext context, AnalyticalResult analysis);
    }
    public class DefaultPromptBuilder: IPromptBuilder
    {
        public string Build(AlertInvestigationContext context, AnalyticalResult analysis)
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

    }
}
