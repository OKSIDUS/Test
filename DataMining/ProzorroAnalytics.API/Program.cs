
using System.Threading.RateLimiting;
using Dapper;
using ProzorroAnalytics.API.BackgroundServices;
using ProzorroAnalytics.Application.Interfaces.Http;
using ProzorroAnalytics.Application.Interfaces.Repositories;
using ProzorroAnalytics.Application.Interfaces.Services;
using ProzorroAnalytics.Application.Options;
using ProzorroAnalytics.Application.Services;
using ProzorroAnalytics.Infrastructure.ApiClients;
using ProzorroAnalytics.Infrastructure.Configuration;
using ProzorroAnalytics.Infrastructure.Http;
using ProzorroAnalytics.Infrastructure.Jobs;
using ProzorroAnalytics.Infrastructure.Persistence;
using ProzorroAnalytics.Infrastructure.Repositories;

namespace ProzorroAnalytics.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddCors(options =>
                options.AddDefaultPolicy(policy =>
                    policy.WithOrigins("http://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod()));

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //Infrastructure
            var connectionString = builder.Configuration.GetConnectionString("Postgres")!;
            builder.Services.AddSingleton(new DbConnectionFactory(connectionString));

            builder.Services.AddScoped<IImportRepository, ProzorroRepository>();
            builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();

            var prozorroOptions = builder.Configuration
                .GetSection(ProzorroApiOptions.SectionName)
                .Get<ProzorroApiOptions>()!;

            builder.Services.AddSingleton<RateLimiter>(_ =>
                new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
                {
                    TokenLimit = prozorroOptions.BurstSize,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                    TokensPerPeriod = prozorroOptions.RequestsPerSecond,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = prozorroOptions.QueueLimit,
                }));
            builder.Services.AddTransient<RateLimitingHandler>();

            builder.Services.AddHttpClient<IProzorroApiClient, ProzorroApiClient>(client =>
            {
                client.BaseAddress = new Uri(prozorroOptions.BaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(prozorroOptions.TimeoutSeconds);
            }).AddHttpMessageHandler<RateLimitingHandler>();

            //Application
            var tenderFilters = builder.Configuration
                .GetSection("TenderFilter")
                .Get<FilterOptions>()!;
            builder.Services.AddSingleton(tenderFilters);
            builder.Services.AddScoped<IImportService, ImportService>();
            builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

            builder.Services.AddSingleton<IImportJobQueue, ImportJobQueue>();
            builder.Services.Configure<NightlyImportOptions>(
                builder.Configuration.GetSection(NightlyImportOptions.SectionName));
            builder.Services.AddHostedService<ImportWorkerService>();
            builder.Services.AddHostedService<NightlyImportService>();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
