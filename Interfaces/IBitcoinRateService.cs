using AkkaNetDemo.Models;

namespace AkkaNetDemo.Interfaces
{
    public interface IBitcoinRateService
    {
        Task<BtcRate> GetRateAsync(
            string date,
            CancellationToken cancellationToken = default);
    }
}
