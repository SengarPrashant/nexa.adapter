using Nexa.Adapter.Services;
using Polly;
using Polly.Extensions.Http;

namespace Nexa.Adapter.Extensions
{
    public static class ServiceExtension
    {

        public static void AddBankDataApiService(this WebApplicationBuilder builder)
        {
            builder.Services.AddHttpClient<IBankDataApiService, BankDataApiService>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5);
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy()); ;
        }


        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3, // keep small
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                // optional logging
            });
        }


        static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(20)
        );
        }
    }


}