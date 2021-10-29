using AdelaideFuel.Services;
using AdelaideFuel.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

using NavigationPage_iOS = Xamarin.Forms.PlatformConfiguration.iOSSpecific.NavigationPage;
using TabbedPage_Droid = Xamarin.Forms.PlatformConfiguration.AndroidSpecific.TabbedPage;
using ToolbarPlacement_Droid = Xamarin.Forms.PlatformConfiguration.AndroidSpecific.ToolbarPlacement;

namespace AdelaideFuel.UI.Services
{
    public class TabbedNavigationService : BaseNavigationService, INavigationService
    {
        private readonly Type[] _tabViewModels = new[]
        {
            typeof(PricesViewModel),
            typeof(MapViewModel),
            typeof(SettingsViewModel)
        };

        private NavigationPage MainPage => (NavigationPage)Application.Current.MainPage;
        private TabbedPage TabbedPage => MainPage?.RootPage as TabbedPage;

        public TabbedNavigationService(ILogger logger) : base(logger)
        {
        }

        public IViewModel TopViewModel => MainPage.CurrentPage is TabbedPage tp
            ? (tp.CurrentPage as NavigationPage)?.RootPage?.BindingContext as IViewModel
            : MainPage.CurrentPage.BindingContext as IViewModel;

        public void Init()
        {
            if (Application.Current.MainPage == null)
            {
                var tabbedPage = new TabbedPage();
                tabbedPage.SetDynamicResource(TabbedPage.StyleProperty, Styles.Keys.BaseTabbedPageStyle);

                TabbedPage_Droid.SetToolbarPlacement(tabbedPage, ToolbarPlacement_Droid.Bottom);
                TabbedPage_Droid.SetIsSwipePagingEnabled(tabbedPage, false);
                NavigationPage.SetHasNavigationBar(tabbedPage, false);

                tabbedPage.Children.Add(new NavigationPage()
                {
                    IconImageSource = ImageSource.FromFile(Application.Current.Resources[Styles.Keys.FuelImg]?.ToString()),
                    Title = Localisation.Resources.Prices,
                });
                tabbedPage.Children.Add(new NavigationPage()
                {
                    IconImageSource = ImageSource.FromFile(Application.Current.Resources[Styles.Keys.MapImg]?.ToString()),
                    Title = Localisation.Resources.Map
                });
                tabbedPage.Children.Add(new NavigationPage()
                {
                    IconImageSource = ImageSource.FromFile(Application.Current.Resources[Styles.Keys.CogImg]?.ToString()),
                    Title = Localisation.Resources.Settings
                });

                ApplyNavigationPageStyle(tabbedPage.Children[0]);
                ApplyNavigationPageStyle(tabbedPage.Children[2]);

                tabbedPage.CurrentPage = tabbedPage.Children.First();

                if (Device.RuntimePlatform == Device.Android)
                {
                    LoadTab(tabbedPage, (NavigationPage)tabbedPage.CurrentPage);
                    // lazy load tabs for the slight start-up gain
                    tabbedPage.CurrentPageChanged += CurrentPageChanged;
                }
                else
                {
                    LoadTab(tabbedPage, (NavigationPage)tabbedPage.Children[0]);
                    LoadTab(tabbedPage, (NavigationPage)tabbedPage.Children[1]);
                    LoadTab(tabbedPage, (NavigationPage)tabbedPage.Children[2]);
                }

                var mainNavPage = new NavigationPage(tabbedPage);
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

                if (_tabViewModels.Contains(vmType))
                {
                    var navTab = (NavigationPage)TabbedPage.Children[_tabViewModels.IndexOf(vmType)];
                    navigatedPage = navTab.RootPage ?? LoadTab(TabbedPage, navTab);
                    navFunc = () =>
                    {
                        TabbedPage.CurrentPage = navTab;
                        return Task.CompletedTask;
                    };
                }
                else
                {
                    navigatedPage = CreatePage<T>();
                    ApplyNavigationPageStyle(navigatedPage);
                    navFunc = () => MainPage.PushAsync(navigatedPage, animated);
                }

                if (navigatedPage != null && parameters?.Any() == true)
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

        private void CurrentPageChanged(object sender, EventArgs e)
        {
            if (sender is TabbedPage tabbedPage &&
                tabbedPage.CurrentPage is NavigationPage navPage)
            {
                if (navPage.RootPage == null)
                    LoadTab(tabbedPage, navPage);

                if (tabbedPage.Children.OfType<NavigationPage>().All(np => np.RootPage != null))
                    tabbedPage.CurrentPageChanged -= CurrentPageChanged;
            }
        }

        private Page LoadTab(TabbedPage tabbedPage, NavigationPage navPage)
        {
            if (navPage.RootPage == null && tabbedPage.Children.Count == _tabViewModels.Length)
            {
                var index = tabbedPage.Children.IndexOf(navPage);
                if (index >= 0)
                {
                    var vmType = _tabViewModels[index];
                    var page = CreatePage(vmType);

                    if (vmType == typeof(MapViewModel))
                        NavigationPage.SetHasNavigationBar(page, false);

                    navPage.PushAsync(page, false);
                    return page;
                }
            }

            return null;
        }

        private static void ApplyNavigationPageStyle(Page page)
        {
            if (Device.RuntimePlatform == Device.iOS && page is NavigationPage navPage)
            {
                NavigationPage_iOS.SetPrefersLargeTitles(navPage, true);
            }
        }
    }
}