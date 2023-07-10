using GliderView.Service;
using GliderView.Service.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Data
{
    public class Configuration
    {
        public static void RegisterServices(IServiceCollection services, IConfiguration config)
        {
            string connectionString = config.GetConnectionString("gliderView")
                ?? throw new InvalidOperationException("Glider View connection string was missing from config.");

            services.AddTransient<IFlightRepository, FlightRepository>(services =>
                new FlightRepository(
                    connectionString,
                    services.GetRequiredService<ILogger<FlightRepository>>()
                )
            );

            services.AddTransient<IUserRepository, UserRepository>(services =>
                new UserRepository(connectionString)
            );

            services.AddTransient<IRateRepo, RateRepo>(services =>
                new RateRepo(
                    connectionString,
                    services.GetRequiredService<IMemoryCache>()
                )
            );

            services.AddTransient<IAirfieldRepo, AirfieldRepo>(services =>
                new AirfieldRepo(
                    connectionString,
                    services.GetRequiredService<IMemoryCache>()
                )
            );
        }
    }
}
