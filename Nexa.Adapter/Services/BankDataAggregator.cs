using Nexa.Adapter.Models;

namespace Nexa.Adapter.Services
{
    public interface IBankDataAggregator
    {
        public Task<AlertInvestigationContext> BuildContextAsync(Alert alert);
    }
    public class BankDataAggregator : IBankDataAggregator
    {
        private readonly IBankDataApiService _bankDataApiService;
        ILogger<BankDataAggregator> _logger;
        public BankDataAggregator(IBankDataApiService bankDataApiService, ILogger<BankDataAggregator> logger)
        {
            _bankDataApiService = bankDataApiService;
            _logger = logger;
        }
        public async Task<AlertInvestigationContext> BuildContextAsync(Alert alert)
        {
            // Fetch primary alert (source of truth)
            var primaryAlert = await _bankDataApiService.GetAlertByIdAsync(alert.Id);

            if (primaryAlert == null)
            {
                _logger.LogError($"Alert not found: {alert.Id}");
                return new AlertInvestigationContext() { Alert = alert };
            }

            var customerId = primaryAlert.CustomerId;

            // Parallelize independent calls (IMPORTANT for performance)
            var alertHistoryTask = _bankDataApiService.GetAlertsByCustomerIdAsync(customerId);
            var transactionsTask = _bankDataApiService.GetTransactionsByCustomerIdAsync(customerId);
            var customerProfileTask = _bankDataApiService.GetCustomerProfileByCustomerIdAsync(customerId);
            var customerBehaviourTask = _bankDataApiService.GetCustomerBehaviourProfileByCustomerIdAsync(customerId);

            await Task.WhenAll(
                alertHistoryTask,
                transactionsTask,
                customerProfileTask,
                customerBehaviourTask
            );

            var alertHistory = alertHistoryTask.Result?.ToList() ?? new List<Alert>();
            var transactions = transactionsTask.Result?.ToList() ?? new List<Transaction>();

            // Determine triggering transaction (best heuristic)
            var triggeringTransaction = transactions
                .OrderByDescending(t => t.Timestamp)
                .FirstOrDefault();

            // Related alerts (same account / same severity)
            var relatedAlerts = alertHistory
                .Where(a =>
                    a.Id != primaryAlert.Id &&
                    (a.AccountNo == primaryAlert.AccountNo ||
                     a.Severity == primaryAlert.Severity))
                .ToList();

            // Build final investigation context
            return new AlertInvestigationContext
            {
                Alert = primaryAlert,

                AlertHistory = alertHistory,

                Transaction = triggeringTransaction,

                TransactionHistory = transactions,

                CustomerProfile = customerProfileTask.Result,

                CustomerBehaviour = customerBehaviourTask.Result,

                RelatedAlerts = relatedAlerts,

                ExternalSignals = new Dictionary<string, object>()
                {
                    // Placeholder – can be enriched later
                    // { "SanctionsHit", false },
                    // { "GeoRisk", "Low" }
                }
            };
        }


    }
}