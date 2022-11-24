using GliderView.Service;
using Hangfire;
using Hangfire.SqlServer;
using NLog.Extensions.Logging;
using NLog.Web;

namespace GliderView.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            IConfiguration configuration = builder.Configuration;

            // Add services to the container.
            ConfigureServices(builder.Services, configuration);

            builder.Logging.ClearProviders();
            
            builder.Host.UseNLog();
            NLog.LogManager.Configuration = new NLogLoggingConfiguration(configuration.GetSection("NLog"));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseCors(policy =>
                {
                    policy.AllowAnyOrigin();
                    policy.AllowAnyHeader();
                });
                app.UseHangfireDashboard();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();


            app.Run();
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration config)
        {
            services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            services.AddHttpClient();

            services.AddTransient<IIgcFileRepository>(services =>
                new IgcFileRepository(config["igcDirectory"]!, services.GetRequiredService<ILogger<IgcFileRepository>>())
            );
            services.AddTransient<IFaaDatabaseProvider, FaaDatabaseProvider>();
            services.AddTransient<IFlightBookClient>(services =>
                new FlightBookClient(config["flightBookApiUrl"]!, services.GetRequiredService<IHttpClientFactory>())
            );
            services.AddTransient<FlightAnalyzer>();
            services.AddTransient<IgcService>();
            services.AddTransient<FlightService>();
            services.AddSingleton<IFlightAnalyzer, FlightAnalyzer>();

            Data.Configuration.RegisterServices(services, config);

            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(config.GetConnectionString("gliderView"), new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true,
                }));

            // Add the processing server as IHostedService
            services.AddHangfireServer(options =>
            {
                // Only have 1 worker to prevent concurrency issues
                options.WorkerCount = 1;
            });
        }
    }
}