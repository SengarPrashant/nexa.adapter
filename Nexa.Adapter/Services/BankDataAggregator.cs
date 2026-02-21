using Nexa.Adapter.Models;

namespace Nexa.Adapter.Services
{
    public interface IBankDataAggregator
    {
        public Task<AlertInvestigationContext> BuildContextAsync(Alert alert);
    }
    public class BankDataAggregator: IBankDataAggregator
    {
        public async Task<AlertInvestigationContext> BuildContextAsync(Alert alert)
        {
            return new AlertInvestigationContext();
        }
    }
}
