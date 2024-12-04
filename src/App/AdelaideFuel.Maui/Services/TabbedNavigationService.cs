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

        private TabbedPage TabbedPage => MainPage?.RootPage as TabbedPage;

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

                ApplyNavigationPageStyle(tabbedPage.Children[2]);

                tabbedPage.CurrentPage = tabbedPage.Children.First();

                if (DeviceInfo.Platform == DevicePlatform.Android)
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

                var navigatedPage = default(Page);
                var navFunc = default(Func<Task>);

                if (_tabViewModels.Contains(vmType))
                {
                    var navTab = (NavigationPage)TabbedPage.Children[Array.IndexOf(_tabViewModels, vmType)];
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

        private void CurrentPageChanged(object sender, EventArgs e)
        {
            if (sender is TabbedPage tabbedPage &&
                tabbedPage.CurrentPage is NavigationPage navPage)
            {
                if (navPage.RootPage is null)
                    LoadTab(tabbedPage, navPage);

                if (tabbedPage.Children.OfType<NavigationPage>().All(np => np.RootPage is not null))
                    tabbedPage.CurrentPageChanged -= CurrentPageChanged;
            }
        }

        private Page LoadTab(TabbedPage tabbedPage, NavigationPage navPage)
        {
            if (navPage.RootPage is null && tabbedPage.Children.Count == _tabViewModels.Length)
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
            if (DeviceInfo.Platform == DevicePlatform.iOS &&
                DeviceInfo.Version.Major > 12 &&
                page is NavigationPage navPage)
            {
                NavigationPage_iOS.SetPrefersLargeTitles(navPage, true);
            }
        }
    }
}