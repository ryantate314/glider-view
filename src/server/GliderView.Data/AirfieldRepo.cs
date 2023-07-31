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
    public class AirfieldRepo : SqlRepository, IAirfieldRepo
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<AirfieldRepo> _logger;

        public AirfieldRepo(string connectionString, IMemoryCache airfieldCache, ILogger<AirfieldRepo> logger) : base(connectionString)
        {
            _cache = airfieldCache;
            _logger = logger;
        }

        public async Task<Airfield?> GetAirfield(string faaId)
        {
            if (faaId == null)
                return null;

            return await _cache.GetOrCreateAsync($"airfield_{faaId}", async (entry) =>
            {
                _logger.LogDebug("Cache miss for airfield {0}", faaId);

                Airfield? field = await _GetAirfield(faaId);

                if (field == null)
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                else
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

                return field;
            });
        }

        private async Task<Airfield?> _GetAirfield(string faaId)
        {
            const string sql = @"
SELECT
    FaaId
    , ElevationMeters
    , Latitude
    , Longitude
FROM dbo.Airfield
WHERE FaaId = @faaId;
";
            using (var con = await GetOpenConnectionAsync())
            {
                return await con.QuerySingleOrDefaultAsync<Airfield>(sql, new { faaId });
            }
        }
    }
}
