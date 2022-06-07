﻿using Acr.UserDialogs;
using AdelaideFuel.Api;
using AdelaideFuel.Services;
using AdelaideFuel.ViewModels;
using Newtonsoft.Json;
using Plugin.InAppBilling;
using Plugin.StoreReview;
using Plugin.StoreReview.Abstractions;
using Refit;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Essentials.Interfaces;

[assembly: AdelaideFuel.Attributes.Preserve]
namespace AdelaideFuel
{
    public static class IoC
    {
        private static readonly Container Container = new Container();

        static IoC()
        {
            Container.Options.EnableAutoVerification = false;

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };

            Container.RegisterSingleton(typeof(IAdelaideFuelApi),
                () => RestService.For<IAdelaideFuelApi>(Constants.ApiUrlBase, new RefitSettings(new NewtonsoftJsonContentSerializer())));

            Container.RegisterSingleton(typeof(IIapVerifyApi),
                () => RestService.For<IIapVerifyApi>(Constants.ApiUrlIapBase, new RefitSettings(new NewtonsoftJsonContentSerializer())));

            Container.RegisterSingleton(typeof(IInAppBilling), () => CrossInAppBilling.Current);
            Container.RegisterSingleton(typeof(IUserDialogs), () => UserDialogs.Instance);
            Container.RegisterSingleton(typeof(IStoreReview), () => CrossStoreReview.Current);

            Container.Register<ILogger, Logger>(Lifestyle.Singleton);
            Container.Register<ICacheService, CacheService>(Lifestyle.Singleton);
            Container.Register<IStoreFactory, StoreFactory>(Lifestyle.Singleton);
            Container.Register<IFuelService, FuelService>(Lifestyle.Singleton);
            Container.Register<IIapVerifyService, IapVerifyService>(Lifestyle.Singleton);
            Container.Register<ISubscriptionService, SubscriptionService>(Lifestyle.Singleton);
            Container.Register<IAppPreferences, AppPreferences>(Lifestyle.Singleton);
            Container.Register<IRetryPolicyFactory, RetryPolicyFactory>(Lifestyle.Singleton);
            Container.Register<IBvmConstructor, BvmConstructor>(Lifestyle.Singleton);

            foreach (var e in GetEssentialInterfaceAndImplementations())
            {
                Container.Register(e.Key, e.Value, Lifestyle.Singleton);
            }

            foreach (var vmType in GetViewModelTypes())
            {
                Container.Register(vmType, vmType, Lifestyle.Transient);
            }
        }

        public static IDictionary<Type, Type> GetEssentialInterfaceAndImplementations()
        {
            var result = new Dictionary<Type, Type>();

            var essentialImpls = typeof(IEssentialsImplementation)
                .Assembly
                .GetTypes()
                .Where(t => t.IsClass && t.Namespace.EndsWith(nameof(Xamarin.Essentials.Implementation)));

            foreach (var impl in essentialImpls)
            {
                var implInterface = impl.GetInterfaces().First(i => i != typeof(IEssentialsImplementation));
                result.Add(implInterface, impl);
            }

            return result;
        }

        public static IEnumerable<Type> GetViewModelTypes()
        {
            return typeof(BaseViewModel)
                .Assembly
                .GetTypes()
                .Where(t => t.IsClass &&
                            !t.IsAbstract &&
                            t.GetInterfaces().Contains(typeof(IViewModel)));
        }

        public static void Verify() => Container.Verify();

        public static T Resolve<T>() where T : class
        {
            return Container.GetInstance<T>();
        }

        public static TViewModel ResolveViewModel<TViewModel>() where TViewModel : class, IViewModel
        {
            return Container.GetInstance<TViewModel>();
        }

        public static void RegisterSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService
        {
            Container.Register<TService, TImplementation>(Lifestyle.Singleton);
        }

        public static void RegisterSingleton(Type serviceType, Type implementationType)
        {
            Container.Register(serviceType, implementationType, Lifestyle.Singleton);
        }

        public static void RegisterSingleton(Type serviceType, Func<object> instanceCreator)
        {
            Container.RegisterSingleton(serviceType, instanceCreator);
        }

        public static void RegisterTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService
        {
            Container.Register<TService, TImplementation>(Lifestyle.Transient);
        }

        public static void RegisterTransient(Type serviceType, Type implementationType)
        {
            Container.Register(serviceType, implementationType, Lifestyle.Transient);
        }

        public static void RegisterTransient(Type serviceType, Func<object> instanceCreator)
        {
            Container.Register(serviceType, instanceCreator, Lifestyle.Transient);
        }
    }
}