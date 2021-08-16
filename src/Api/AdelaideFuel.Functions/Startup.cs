using AdelaideFuel.Api;
using AdelaideFuel.Functions.Services;
using AdelaideFuel.TableStore.Entities;
using AdelaideFuel.TableStore.Repositories;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Polly;
using Refit;
using System;
using CloudTableStorageAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount;

[assembly: FunctionsStartup(typeof(AdelaideFuel.Functions.Startup))]

namespace AdelaideFuel.Functions
{
    public class Startup : FunctionsStartup
    {
        private readonly static string FuelApiUrl = Environment.GetEnvironmentVariable(nameof(FuelApiUrl)) ?? string.Empty;
        private readonly static string SubscriberToken = Environment.GetEnvironmentVariable(nameof(SubscriberToken)) ?? string.Empty;

        public override void Configure(IFunctionsHostBuilder builder)
        {
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

            string getConnectionString(IServiceProvider serviceProvider)
            {
                var config = serviceProvider.GetService<IConfiguration>();
                var storageConnString =
                    config["Values:AzureWebJobsStorage"] ??
                    config.GetConnectionString("StorageConnectionString") ??
                    Environment.GetEnvironmentVariable("AzureWebJobsStorage") ??
                    "UseDevelopmentStorage=true;";
                return storageConnString;
            }

            builder.Services.AddSingleton(serviceProvider =>
                CloudTableStorageAccount.Parse(getConnectionString(serviceProvider)));

            builder.Services.AddSingleton(serviceProvider =>
            {
                var storageAccount = serviceProvider.GetService<CloudTableStorageAccount>();
                return storageAccount.CreateCloudTableClient();
            });

            builder.Services.AddSingleton<IBlobService>(sp => new BlobService(getConnectionString(sp)));
            builder.Services.AddSingleton<ITableRepository<BrandEntity>, TableRepository<BrandEntity>>();
            builder.Services.AddSingleton<ITableRepository<FuelEntity>, TableRepository<FuelEntity>>();
            builder.Services.AddSingleton<ITableRepository<GeographicRegionEntity>, TableRepository<GeographicRegionEntity>>();
            builder.Services.AddSingleton<ITableRepository<SiteEntity>, TableRepository<SiteEntity>>();
            builder.Services.AddSingleton<ITableRepository<SitePriceEntity>, TableRepository<SitePriceEntity>>();
            builder.Services.AddSingleton<ITableRepository<SitePriceArchiveEntity>, TableRepository<SitePriceArchiveEntity>>();
        }
    }
}