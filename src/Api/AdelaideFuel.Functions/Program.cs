using AdelaideFuel.Api;
using AdelaideFuel.Functions.Models;
using AdelaideFuel.Functions.Services;
using AdelaideFuel.TableStore.Entities;
using AdelaideFuel.TableStore.Models;
using AdelaideFuel.TableStore.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Refit;
using System;
using System.Text.Json.Serialization;
using JsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

string FuelApiUrl = Environment.GetEnvironmentVariable(nameof(FuelApiUrl)) ?? string.Empty;
string SubscriberToken = Environment.GetEnvironmentVariable(nameof(SubscriberToken)) ?? string.Empty;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.Configure<JsonOptions>(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        services.AddSingleton(serviceProvider =>
        {
            return new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables()
               .Build();
        });

        services.AddRefitClient<ISaFuelPricingApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(FuelApiUrl))
            .AddHttpMessageHandler(() => new FuelPriceAuthHeaderHandler(SubscriberToken))
            .AddTransientHttpErrorPolicy(b => b.WaitAndRetryAsync(3, n => TimeSpan.FromSeconds(Math.Pow(2, n))));

        services.AddSingleton(services =>
            new TableStorageOptions()
            {
                AzureWebJobsStorage = services.GetService<IConfiguration>().GetValue<string>("AzureWebJobsStorage")
            });

        services.AddSingleton(services =>
            new BlobStorageOptions()
            {
                AzureWebJobsStorage = services.GetService<IConfiguration>().GetValue<string>("AzureWebJobsStorage"),
                BlobContainerName = services.GetService<IConfiguration>().GetValue<string>("BlobContainerName")
            });

        services.AddOptions<SendGridOptions>()
            .Configure<IConfiguration>((settings, config) => config.GetSection("SendGrid").Bind(settings));

        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<IBlobService, BlobService>();
        services.AddSingleton<ISendGridService, SendGridService>();
        services.AddSingleton<ITableRepository<BrandEntity>, TableRepository<BrandEntity>>();
        services.AddSingleton<ITableRepository<FuelEntity>, TableRepository<FuelEntity>>();
        services.AddSingleton<ITableRepository<GeographicRegionEntity>, TableRepository<GeographicRegionEntity>>();
        services.AddSingleton<ITableRepository<SiteEntity>, TableRepository<SiteEntity>>();
        services.AddSingleton<ITableRepository<SitePriceEntity>, TableRepository<SitePriceEntity>>();
        services.AddSingleton<ITableRepository<SitePriceArchiveEntity>, TableRepository<SitePriceArchiveEntity>>();
        services.AddSingleton<ITableRepository<SiteExceptionEntity>, TableRepository<SiteExceptionEntity>>();
        services.AddSingleton<ITableRepository<SitePriceExceptionLogEntity>, TableRepository<SitePriceExceptionLogEntity>>();
    })
    .Build();

host.Run();