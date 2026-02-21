using Nexa.Adapter.Models;

namespace Nexa.Adapter.Services
{
    public interface IEvidenceWeightingEngine
    {
        public EvidenceResult Evaluate(AlertInvestigationContext input);
    }
    public class EvidenceWeightingEngine: IEvidenceWeightingEngine
    {
        public EvidenceResult Evaluate(AlertInvestigationContext input)
        {
            return new EvidenceResult
            {
                TransactionPatternConsistency = EvaluateTransactionPattern(input),
                HistoricalBehaviorAlignment = EvaluateBehaviorAlignment(input),
                BeneficiaryRisk = EvaluateBeneficiaryRisk(input),
                VelocityAnomaly = EvaluateVelocity(input),
            };
        }

        private EvidenceStrength EvaluateTransactionPattern(AlertInvestigationContext input)
        {
            if (input.TransactionHistory == null || !input.TransactionHistory.Any())
                return EvidenceStrength.None;

            var avgAmount = input.TransactionHistory.Average(t => t.Amount);

            if (input.Alert.Amount <= avgAmount * Convert.ToDecimal(1.2))
                return EvidenceStrength.Strong;

            if (input.Alert.Amount <= avgAmount * 2)
                return EvidenceStrength.Moderate;

            return EvidenceStrength.Weak;
        }

        private EvidenceStrength EvaluateBehaviorAlignment(AlertInvestigationContext input)
        {
            if (input.CustomerBehaviour == null)
                return EvidenceStrength.None;

            var behaviour = input.CustomerBehaviour;
            var alertAmount = input.Alert.Amount;

            // Amount comparison
            if (alertAmount <= behaviour.MaxTransactionAmount * Convert.ToDecimal(1.1))
                return EvidenceStrength.Strong;

            if (alertAmount <= behaviour.MaxTransactionAmount * 2)
                return EvidenceStrength.Moderate;

            return EvidenceStrength.Weak;
        }

        private EvidenceStrength EvaluateVelocity(AlertInvestigationContext input)
        {
            if (input.TransactionHistory == null || input.TransactionHistory.Count == 0)
                return EvidenceStrength.None;

            var txn = input.Transaction;
            var history = input.TransactionHistory;

            // Configurable window (banks LOVE configurable thresholds)
            var windowStart = txn.Timestamp.AddHours(-1);

            var recentTransactions = history
                .Where(t => t.Timestamp >= windowStart)
                .ToList();

            if (recentTransactions.Count == 0)
                return EvidenceStrength.None;

            var transactionCount = recentTransactions.Count;
            var aggregatedAmount = recentTransactions.Sum(t => t.Amount);

            /*
                Bank-realistic velocity logic:

                Strong → burst behaviour
                Moderate → meaningful clustering
                Weak → mild deviation
            */

            if (transactionCount >= 10)
                return EvidenceStrength.Strong;

            if (aggregatedAmount >= txn.Amount * 3)
                return EvidenceStrength.Moderate;

            if (transactionCount >= 5)
                return EvidenceStrength.Weak;

            return EvidenceStrength.None;
        }

        private EvidenceStrength EvaluateBeneficiaryRisk(AlertInvestigationContext input)
        {
            if (input.TransactionHistory == null || input.TransactionHistory.Count == 0)
                return EvidenceStrength.None;

            var txn = input.Transaction;

            var matchingTransactions = input.TransactionHistory
                .Where(t =>
                    NormalizeAccount(t.DestinationAccount) ==
                    NormalizeAccount(txn.DestinationAccount))
                .ToList();

            int priorTxnCount = matchingTransactions.Count;

            /*
                Bank-realistic beneficiary logic:

                Strong → Completely new counterparty
                Moderate → Rare counterparty
                Weak → Low-frequency but known
                None → Established counterparty
            */

            if (priorTxnCount == 0)
                return EvidenceStrength.Strong;

            if (priorTxnCount <= 2)
                return EvidenceStrength.Moderate;

            if (priorTxnCount <= 5)
                return EvidenceStrength.Weak;

            return EvidenceStrength.None;
        }
       
        private string NormalizeAccount(string account)
        {
            return account?.Replace(" ", "")
                           .ToUpperInvariant();
        }
    }

}
