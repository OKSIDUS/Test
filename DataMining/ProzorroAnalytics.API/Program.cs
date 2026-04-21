
using ProzorroAnalytics.Application.Interfaces.Http;
using ProzorroAnalytics.Application.Interfaces.Repositories;
using ProzorroAnalytics.Application.Interfaces.Services;
using ProzorroAnalytics.Application.Options;
using ProzorroAnalytics.Application.Services;
using ProzorroAnalytics.Infrastructure.ApiClients;
using ProzorroAnalytics.Infrastructure.Configuration;
using ProzorroAnalytics.Infrastructure.Repositories;

namespace ProzorroAnalytics.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //Infrastructure
            builder.Services.AddScoped<IImportRepository, ProzorroRepository>();
            builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();

            var prozorroOptions = builder.Configuration
                .GetSection(ProzorroApiOptions.SectionName)
                .Get<ProzorroApiOptions>()!;

            builder.Services.AddHttpClient<IProzorroApiClient, ProzorroApiClient>(client =>
            {
                client.BaseAddress = new Uri(prozorroOptions.BaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(prozorroOptions.TimeoutSeconds);
            });

            //Application
            var tenderFilters = builder.Configuration
                .GetSection("TenderFilter")
                .Get<FilterOptions>()!;
            builder.Services.AddSingleton(tenderFilters);
            builder.Services.AddScoped<IImportService, ImportService>();
            builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
