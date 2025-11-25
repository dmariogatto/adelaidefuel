using AdelaideFuel.Maui.Views;
using AdelaideFuel.Services;
using AdelaideFuel.ViewModels;
using System.Reflection;
using NavigationPage_iOS = Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.NavigationPage;
using TabbedPage_Droid = Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.TabbedPage;
using ToolbarPlacement_Droid = Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.ToolbarPlacement;

namespace AdelaideFuel.Maui.Services
{
    public class TabbedNavigationService : BaseNavigationService, INavigationService
    {
        private readonly Type[] _tabViewModels = new[]
        {
            typeof(PricesViewModel),
            typeof(MapViewModel),
            typeof(SettingsViewModel)
        };

        private NavigationPage MainPage
        {
            get
            {
                if (Application.Current.Windows.Count == 0)
                    throw new InvalidOperationException("Application does not have any Windows initialised");
                if (Application.Current.Windows[0].Page is not NavigationPage)
                    throw new InvalidOperationException("Window[0].Page is not a NavigationPage");

                return (NavigationPage)Application.Current.Windows[0].Page;
            }
        }

        private MainTabbedPage TabbedPage => MainPage?.RootPage as MainTabbedPage;

        public TabbedNavigationService(ILogger logger) : base(logger)
        {
        }

        public IViewModel TopViewModel => MainPage.CurrentPage is TabbedPage tp
            ? (tp.CurrentPage as NavigationPage)?.RootPage?.BindingContext as IViewModel
            : MainPage.CurrentPage.BindingContext as IViewModel;

        public override Page CreateMainPage()
        {
            if (Application.Current.Windows.Count == 0)
            {
                var tabbedPage = new MainTabbedPage();

                NavigationPage.SetHasNavigationBar(tabbedPage, false);

                var mainNavPage = new NavigationPage(tabbedPage);
                mainNavPage.Popped += OnMainNavigationPopped;
                return mainNavPage;
            }

            return MainPage;
        }

        public async Task NavigateToAsync<T>(IDictionary<string, string> parameters = null, bool animated = true) where T : IViewModel
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var vmType = typeof(T);

                var navigatedPage = default(IView);
                var navFunc = default(Func<Task>);

                if (TabbedPage.GetIndexForViewModel(vmType) is var i and >= 0)
                {
                    navigatedPage = TabbedPage.GetTabForIndex(i);
                    navFunc = () =>
                    {
                        TabbedPage.SelectedIndex = i;
                        return Task.CompletedTask;
                    };
                }
                else
                {
                    navigatedPage = CreatePage<T>();
                    navFunc = () => MainPage.PushAsync(navigatedPage as Page, animated);
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
                var pages = MainPage.Navigation.NavigationStack.Skip(1).OfType<IBasePage>().ToList();

                await MainPage.PopToRootAsync(animated);

                foreach (var page in pages)
                {
                    page.OnDestroy();
                }
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

        private void OnMainNavigationPopped(object sender, NavigationEventArgs e)
        {
            try
            {
                if (e.Page is IBasePage page)
                {
                    page.OnDestroy();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}