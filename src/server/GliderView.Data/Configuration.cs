using GliderView.Service;
using GliderView.Service.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            services.AddTransient<IFlightRepository, FlightRepository>(services =>
                new FlightRepository(config.GetConnectionString("gliderView")!)
            );

            services.AddTransient<IUserRepository, UserRepository>(services =>
                new UserRepository(config.GetConnectionString("gliderView")!)
            );
        }
    }
}
