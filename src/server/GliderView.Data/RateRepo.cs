using Dapper;
using GliderView.Service.Models;
using GliderView.Service.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Data
{
    public class RateRepo : SqlRepository, IRateRepo
    {
        private readonly IMemoryCache _rateCache;
        private readonly ILogger<RateRepo> _logger;

        public RateRepo(string connectionString, IMemoryCache rateCache, ILogger<RateRepo> logger) : base(connectionString)
        {
            _rateCache = rateCache;
            _logger = logger;
        }

        public Task<Rates?> GetRates()
        {
            return _rateCache.GetOrCreateAsync("current_rates", async (entry) =>
            {
                _logger.LogDebug("Cache miss for tow rates");

                Rates? rates = await _GetRates();

                if (rates != null)
                    entry.AbsoluteExpiration = (rates.ExpirationDate == null || DateTime.UtcNow.AddHours(1) < rates.ExpirationDate)
                        ? DateTime.UtcNow.AddHours(1)
                        : rates.ExpirationDate;
                else
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                return rates;
            });
        }

        public Task<AircraftRates?> GetAircraftRates(Guid aircraftId)
        {
            return _rateCache.GetOrCreateAsync($"aircraft_rate_{aircraftId}", async (entry) =>
            {
                _logger.LogDebug("Cache miss for aircraft rates: {0}", aircraftId);

                AircraftRates? rates = await _GetAircraftRates(aircraftId);

                if (rates != null)
                    entry.AbsoluteExpiration = (rates.ExpirationDate == null || DateTime.UtcNow.AddHours(1) < rates.ExpirationDate)
                        ? DateTime.UtcNow.AddHours(1)
                        : rates.ExpirationDate;
                else
                    entry.AbsoluteExpiration = DateTime.UtcNow.AddMinutes(10);

                return rates;
            });
        }

        private async Task<Rates?> _GetRates()
        {
            const string sql = @"
SELECT
    R.EffectiveDate
    , R.ExpirationDate
    , R.HookupCost
    , R.CostPerHundredFeet
    , R.MinTowHeight
FROM dbo.Rates R
WHERE R.EffectiveDate <= @now
    AND (R.ExpirationDate IS NULL OR R.ExpirationDate > @now)
";
            using (var con = await GetOpenConnectionAsync())
            {
                return await con.QuerySingleOrDefaultAsync<Rates>(sql, new { now = DateTime.UtcNow });
            }
        }

        private async Task<AircraftRates?> _GetAircraftRates(Guid aircraftId)
        {
            const string sql = @"
SELECT
    A.AircraftGuid AS AircraftId
    , R.EffectiveDate
    , R.ExpirationDate
    , R.RentalCostPerHour
    , R.MinRentalHours
FROM dbo.AircraftRates R
    JOIN dbo.Aircraft A
        ON R.AircraftId = A.AircraftId
WHERE A.IsDeleted = 0
    AND A.AircraftGuid = @aircraftId
    AND R.EffectiveDate <= @now
    AND (R.ExpirationDate IS NULL OR R.ExpirationDate > @now)
";
            using (var con = await GetOpenConnectionAsync())
            {
                var args = new
                {
                    aircraftId,
                    now = DateTime.UtcNow
                };
                return await con.QuerySingleOrDefaultAsync<AircraftRates>(sql, args);
            }
        }
    }
}
