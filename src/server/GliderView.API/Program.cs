using GliderView.Data;
using GliderView.Service;
using GliderView.Service.Models;
using GliderView.Service.Repositories;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using NLog.Extensions.Logging;
using NLog.Web;
using System.IO.Abstractions;
using System.Text;

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
                    policy.WithOrigins("http://localhost:4200")
                        .AllowCredentials()
                        .AllowAnyHeader();
                });
                app.UseHangfireDashboard();
            }

            // Because Docker is behind a Nginx reverse proxy, we don't need SSL
            //app.UseHttpsRedirection();

            app.UseAuthentication();
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
                new IgcFileRepository(
                    config["igcDirectory"]!,
                    services.GetRequiredService<ILogger<IgcFileRepository>>(),
                    new FileSystem()
                )
            );
            services.AddTransient<IFaaDatabaseProvider, FaaDatabaseProvider>();
            services.AddTransient<IFlightBookClient>(services =>
                new FlightBookClient(config["flightBookApiUrl"]!, services.GetRequiredService<IHttpClientFactory>())
            );
            services.AddTransient<FlightAnalyzer>();
            services.AddTransient<IgcService>();
            services.AddTransient<FlightService>();
            services.AddSingleton<IFlightAnalyzer, FlightAnalyzer>();
            services.AddTransient<UserService>();
            services.AddTransient<IPasswordHasher<User>, PasswordHasher<User>>();

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

            // Setup JWT Authentication
            var jwtSettings = new JwtSettings()
            {
                Audience = config["Jwt:Audience"],
                Issuer = config["Jwt:Issuer"],
                AuthTokenLifetime = Int32.Parse(config["Jwt:AuthTokenLifetime"]),
                RefreshTokenLifetime = Int32.Parse(config["Jwt:RefreshTokenLifetime"]),
                RefreshSecurityKey = config["Jwt:RefreshSecurityKey"]!,
                AuthSecurityKey = config["Jwt:AuthSecurityKey"]!
            };
            services.AddSingleton(jwtSettings);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.AuthSecurityKey)),
                };
            });

            services.AddSingleton<TokenGenerator>();
            services.AddSingleton<TokenValidator>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(Scopes.CreateUser, policy =>
                    policy.RequireClaim(Scopes.CreateUser));
            });
        }
    }
}