using AdelaideFuel.Services;
using AdelaideFuel.ViewModels;
using System.Reflection;

namespace AdelaideFuel.Maui.Services
{
    public class PageNavigationService : BaseNavigationService, INavigationService
    {
        private readonly Dictionary<Type, Page> _mainPages = new Dictionary<Type, Page>()
        {
            { typeof(PricesViewModel), null },
            { typeof(MapViewModel), null},
            { typeof(SettingsViewModel), null }
        };

        private NavigationPage MainPage => (NavigationPage)Application.Current.MainPage;

        public PageNavigationService(ILogger logger) : base(logger)
        {
        }

        public IViewModel TopViewModel => MainPage.CurrentPage?.BindingContext as IViewModel;

        public void Init()
        {
            if (Application.Current.MainPage is null)
            {
                var mainNavPage = new NavigationPage(GetMainPage<PricesViewModel>());
                Application.Current.MainPage = mainNavPage;
            }
        }

        public async Task NavigateToAsync<T>(IDictionary<string, string> parameters = null, bool animated = true) where T : IViewModel
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var vmType = typeof(T);

                var navigatedPage = default(Page);
                var navFunc = default(Func<Task>);

                if (GetMainPage<T>() is Page mainPage)
                {
                    navigatedPage = mainPage;
                    navFunc = navigatedPage == MainPage.RootPage
                        ? (Func<Task>)(() => MainPage.PopToRootAsync(animated))
                        : (Func<Task>)(() => MainPage.PushAsync(navigatedPage, animated));
                }
                else
                {
                    navigatedPage = CreatePage<T>();
                    navFunc = () => MainPage.PushAsync(navigatedPage, animated);
                }

                if (navigatedPage is not null && parameters?.Any() == true)
                {
                    var pageType = navigatedPage.GetType();
                    var qProps = pageType.GetCustomAttributes(false).OfType<QueryPropertyAttribute>();
                    foreach (var qp in qProps)
                    {
                        if (parameters.TryGetValue(qp.QueryId, out var val) &&
                            pageType.GetProperty(qp.Name) is PropertyInfo pi &&
                            pi.CanWrite)
                        {
                            pi.SetValue(navigatedPage, val);
                        }
                    }
                }

                await navFunc.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task PopAsync(bool animated = true)
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                await MainPage.PopAsync(animated);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task PopToRootAsync(bool animated = true)
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                await MainPage.PopToRootAsync(animated);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Page GetMainPage<T>() where T : IViewModel
        {
            var vmType = typeof(T);

            if (_mainPages.ContainsKey(vmType))
            {
                var page = _mainPages[vmType] ?? CreatePage(vmType);
                _mainPages[vmType] = page;

                return page;
            }

            return null;
        }
    }
}