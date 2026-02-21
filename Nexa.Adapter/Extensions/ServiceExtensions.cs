using Nexa.Adapter.Services;
using Polly;
using Polly.Extensions.Http;

namespace Nexa.Adapter.Extensions
{
    public static class ServiceExtensions
    {
        extension(WebApplicationBuilder builder)
        {
            public void AddBankDataApiService()
            {
                builder.Services.AddHttpClient<IBankDataApiService, BankDataApiService>(client =>
                {
                    client.BaseAddress = new Uri(builder.Configuration["BankBaseUrl"]);
                }).AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy()); ;
            }
        }

        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }


        static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
        }
    }


}
