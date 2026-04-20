
using ProzorroAnalytics.Application.Interfaces.Repositories;
using ProzorroAnalytics.Application.Interfaces.Services;
using ProzorroAnalytics.Application.Services;
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

            //Application
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
