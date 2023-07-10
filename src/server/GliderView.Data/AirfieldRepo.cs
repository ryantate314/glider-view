using Dapper;
using GliderView.Service.Models;
using GliderView.Service.Repositories;
using Microsoft.Extensions.Caching.Memory;
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

        public AirfieldRepo(string connectionString, IMemoryCache airfieldCache) : base(connectionString)
        {
            _cache = airfieldCache;
        }

        public async Task<Airfield?> GetAirfield(string faaId)
        {
            if (faaId == null)
                return null;

            return await _cache.GetOrCreateAsync($"airfield_{faaId}", async (entry) =>
            {
                Airfield? field = await _GetAirfield(faaId);

                if (field == null)
                    entry.AbsoluteExpiration = DateTime.UtcNow.AddMinutes(10);
                else
                    entry.AbsoluteExpiration = DateTime.UtcNow.AddHours(24);

                return field;
            });
        }

        private async Task<Airfield?> _GetAirfield(string faaId)
        {
            const string sql = @"
SELECT
    FaaId
    , ElevationMeters
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
