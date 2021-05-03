using AdelaideFuel.Services;
using AdelaideFuel.UI.Views;
using AdelaideFuel.ViewModels;
using System;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Services
{
    public abstract class BaseNavigationService
    {
        protected readonly ILogger Logger;

        public BaseNavigationService(ILogger logger)
        {
            Logger = logger;
        }

        public bool IsBusy { get; protected set; }

        protected static Page CreatePage<T>() where T : IViewModel
            => CreatePage(typeof(T));

        protected static Page CreatePage(Type vmType)
        {
            if (!typeof(IViewModel).IsAssignableFrom(vmType))
                throw new ArgumentException($"Must be an instance of '{nameof(IViewModel)}'", nameof(vmType));

            var pageType = GetPageType(vmType);
            return Activator.CreateInstance(pageType) as Page;
        }

        protected static Type GetPageType<T>() where T : IViewModel
            => GetPageType(typeof(T));

        private static Type GetPageType(Type vmType)
        {
            var vmName = vmType.Name;
            var basePageType = typeof(BasePage<>);
            var pageName = $"{basePageType.Namespace}.{vmName.Replace("ViewModel", string.Empty)}";
            return Type.GetType($"{pageName}Page, {basePageType.Assembly.GetName()}");
        }
    }
}