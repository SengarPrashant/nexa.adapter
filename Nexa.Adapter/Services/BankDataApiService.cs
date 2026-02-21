using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Nexa.Adapter.Configuration;
using Nexa.Adapter.Models;

namespace Nexa.Adapter.Services
{

    public interface IBankDataApiService
    {
        // ALERTS
        Task<IList<Alert>> GetAlertsAsync();
        Task<Alert?> GetAlertByIdAsync(int id);
        Task<IList<Alert>> GetAlertsBySeverityAsync(string severity);
        Task<IList<Alert>> GetAlertsByCustomerIdAsync(int customerId);

        // CRM INSIGHTS
        Task<IList<object>> GetCrmInsightsAsync();
        Task<IList<object>> GetCrmInsightsByCustomerIdAsync(int customerId);
        Task<IList<object>> GetHighPriorityCrmInsightsAsync();

        // CUSTOMER PROFILES
        Task<IList<CustomerProfile>> GetCustomerProfilesAsync();
        Task<CustomerProfile?> GetCustomerProfileByCustomerIdAsync(int customerId);

        // CUSTOMER BEHAVIOUR
        Task<IList<CustomerBehaviourProfile>> GetCustomerBehaviourProfilesAsync();
        Task<CustomerBehaviourProfile?> GetCustomerBehaviourProfileByCustomerIdAsync(int customerId);

        // TRANSACTIONS
        Task<IList<Transaction>> GetTransactionsAsync();
        Task<IList<Transaction>> GetTransactionsByCustomerIdAsync(int customerId);
        Task<IList<Transaction>> GetTransactionsByStatusAsync(string status);

        // Customer Risk Summary
        Task<object> GetCustomerRiskSummaryAsync(int customerId);
    }
    public class BankDataApiService : IBankDataApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _bankApiBaseUrl;
        private readonly ILogger<BankDataApiService> _logger;

        public BankDataApiService(
            HttpClient httpClient,
            IOptions<BankDataApiOptions> bankDataApiOptions,
            ILogger<BankDataApiService> logger)
        {
            _httpClient = httpClient;
            _bankApiBaseUrl = bankDataApiOptions.Value.BankBaseurl;
            _logger = logger;
        }

        // ALERTS

        public async Task<IList<Alert>> GetAlertsAsync()
            => await GetBankApiResponse<List<Alert>>($"{_bankApiBaseUrl}/alerts");

        public async Task<Alert?> GetAlertByIdAsync(int id)
            => await GetBankApiResponse<Alert>($"{_bankApiBaseUrl}/alerts/{id}");

        public async Task<IList<Alert>> GetAlertsBySeverityAsync(string severity)
            => await GetBankApiResponse<List<Alert>>(
                $"{_bankApiBaseUrl}/alerts/by-severity/{severity}");

        public async Task<IList<Alert>> GetAlertsByCustomerIdAsync(int customerId)
            => await GetBankApiResponse<List<Alert>>(
                $"{_bankApiBaseUrl}/customers/{customerId}/alerts");


        //CRM INSIGHTS

        public async Task<IList<object>> GetCrmInsightsAsync()
            => await GetBankApiResponse<List<object>>(
                $"{_bankApiBaseUrl}/crm-insights");

        public async Task<IList<object>> GetCrmInsightsByCustomerIdAsync(int customerId)
            => await GetBankApiResponse<List<object>>(
                $"{_bankApiBaseUrl}/customers/{customerId}/crm-insights");

        public async Task<IList<object>> GetHighPriorityCrmInsightsAsync()
            => await GetBankApiResponse<List<object>>(
                $"{_bankApiBaseUrl}/crm-insights/high-priority");



        // CUSTOMER PROFILES

        public async Task<IList<CustomerProfile>> GetCustomerProfilesAsync()
            => await GetBankApiResponse<List<CustomerProfile>>(
                $"{_bankApiBaseUrl}/customers/profiles");

        public async Task<CustomerProfile?> GetCustomerProfileByCustomerIdAsync(int customerId)
            => await GetBankApiResponse<CustomerProfile>(
                $"{_bankApiBaseUrl}/customers/{customerId}/profile");



        // CUSTOMER BEHAVIOUR


        public async Task<IList<CustomerBehaviourProfile>> GetCustomerBehaviourProfilesAsync()
            => await GetBankApiResponse<List<CustomerBehaviourProfile>>(
                $"{_bankApiBaseUrl}/customers/behaviours");

        public async Task<CustomerBehaviourProfile?> GetCustomerBehaviourProfileByCustomerIdAsync(int customerId)
            => await GetBankApiResponse<CustomerBehaviourProfile>(
                $"{_bankApiBaseUrl}/customers/{customerId}/behaviour");



        // TRANSACTIONS

        public async Task<IList<Transaction>> GetTransactionsAsync()
            => await GetBankApiResponse<List<Transaction>>(
                $"{_bankApiBaseUrl}/transactions");

        public async Task<IList<Transaction>> GetTransactionsByCustomerIdAsync(int customerId)
            => await GetBankApiResponse<List<Transaction>>(
                $"{_bankApiBaseUrl}/customers/{customerId}/transactions");

        public async Task<IList<Transaction>> GetTransactionsByStatusAsync(string status)
            => await GetBankApiResponse<List<Transaction>>(
                $"{_bankApiBaseUrl}/transactions/by-status/{status}");



        // Risk Summary

        public async Task<object> GetCustomerRiskSummaryAsync(int customerId)
            => await GetBankApiResponse<object>(
                $"{_bankApiBaseUrl}/customers/{customerId}/risk-summary");



        // COMMON HTTP HANDLER

        private async Task<TResponse> GetBankApiResponse<TResponse>(string request)
        {
            try
            {
                var response = await _httpClient.GetAsync(request);
                _logger.LogInformation($"For request#{request} api received response with status code {response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                    return default!;

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TResponse>(content)!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bank API call failed: {Request}", request);
                throw;
            }
        }
    }
}