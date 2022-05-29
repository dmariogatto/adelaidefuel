using AdelaideFuel.Api;
using AdelaideFuel.Functions.Models;
using AdelaideFuel.Functions.Services;
using AdelaideFuel.TableStore.Entities;
using AdelaideFuel.TableStore.Models;
using AdelaideFuel.TableStore.Repositories;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Polly;
using Refit;
using System;

[assembly: FunctionsStartup(typeof(AdelaideFuel.Functions.Startup))]

namespace AdelaideFuel.Functions
{
    public class Startup : FunctionsStartup
    {
        private readonly static string FuelApiUrl = Environment.GetEnvironmentVariable(nameof(FuelApiUrl)) ?? string.Empty;
        private readonly static string SubscriberToken = Environment.GetEnvironmentVariable(nameof(SubscriberToken)) ?? string.Empty;

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = builder.GetContext().Configuration;

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore
            };

            builder.Services.AddSingleton(serviceProvider =>
            {
                return new ConfigurationBuilder()
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                   .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables()
                   .Build();
            });

            builder.Services.AddRefitClient<ISaFuelPricingApi>(
                    new RefitSettings(new NewtonsoftJsonContentSerializer(new JsonSerializerSettings()
                    {
                        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                        NullValueHandling = NullValueHandling.Ignore
                    })))
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(FuelApiUrl))
                .AddHttpMessageHandler(() => new FuelPriceAuthHeaderHandler(SubscriberToken))
                .AddTransientHttpErrorPolicy(b => b.WaitAndRetryAsync(3, n => TimeSpan.FromSeconds(Math.Pow(2, n))));

            builder.Services.AddSingleton(services =>
                new TableStorageOptions()
                {
                    AzureWebJobsStorage = services.GetService<IConfiguration>().GetValue<string>("AzureWebJobsStorage")
                });

            builder.Services.AddSingleton(services =>
                new BlobStorageOptions()
                {
                    AzureWebJobsStorage = services.GetService<IConfiguration>().GetValue<string>("AzureWebJobsStorage"),
                    BlobContainerName = services.GetService<IConfiguration>().GetValue<string>("BlobContainerName")
                });

            builder.Services.AddOptions<SendGridOptions>()
                .Configure(configuration.GetSection("SendGrid").Bind);

            builder.Services.AddSingleton<ICacheService, CacheService>();
            builder.Services.AddSingleton<IBlobService, BlobService>();
            builder.Services.AddSingleton<ISendGridService, SendGridService>();
            builder.Services.AddSingleton<ITableRepository<BrandEntity>, TableRepository<BrandEntity>>();
            builder.Services.AddSingleton<ITableRepository<FuelEntity>, TableRepository<FuelEntity>>();
            builder.Services.AddSingleton<ITableRepository<GeographicRegionEntity>, TableRepository<GeographicRegionEntity>>();
            builder.Services.AddSingleton<ITableRepository<SiteEntity>, TableRepository<SiteEntity>>();
            builder.Services.AddSingleton<ITableRepository<SitePriceEntity>, TableRepository<SitePriceEntity>>();
            builder.Services.AddSingleton<ITableRepository<SitePriceArchiveEntity>, TableRepository<SitePriceArchiveEntity>>();
            builder.Services.AddSingleton<ITableRepository<SiteExceptionEntity>, TableRepository<SiteExceptionEntity>>();
            builder.Services.AddSingleton<ITableRepository<SitePriceExceptionLogEntity>, TableRepository<SitePriceExceptionLogEntity>>();
        }
    }
}