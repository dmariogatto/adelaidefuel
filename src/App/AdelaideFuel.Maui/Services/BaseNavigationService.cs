using AdelaideFuel.Maui.Views;
using AdelaideFuel.Services;
using AdelaideFuel.ViewModels;

namespace AdelaideFuel.Maui.Services
{
    public static class BaseNavigationServiceExtensions
    {
        public static Page GetMainPage(this INavigationService navigationService)
            => navigationService is BaseNavigationService service
               ? service.CreateMainPage()
               : null;
    }


    public abstract class BaseNavigationService
    {
        protected readonly ILogger Logger;

        private readonly Dictionary<Type, Type> _vmToPageTypes = new Dictionary<Type, Type>();

        public BaseNavigationService(ILogger logger)
        {
            Logger = logger;
        }

        public abstract Page CreateMainPage();

        public bool IsBusy { get; protected set; }

        protected Page CreatePage<T>() where T : IViewModel
            => CreatePage(typeof(T));

        protected Page CreatePage(Type vmType)
        {
            if (!_vmToPageTypes.TryGetValue(vmType, out var pageType))
            {
                if (!typeof(IViewModel).IsAssignableFrom(vmType))
                    throw new ArgumentException($"Must be an instance of '{nameof(IViewModel)}'", nameof(vmType));

                pageType = GetPageType(vmType);
                _vmToPageTypes[vmType] = pageType;
            }

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