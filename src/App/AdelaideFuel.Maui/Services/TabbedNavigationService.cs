using AdelaideFuel.Maui.Views;
using AdelaideFuel.Services;
using AdelaideFuel.ViewModels;
using System.Reflection;

namespace AdelaideFuel.Maui.Services
{
    public class TabbedNavigationService : BaseNavigationService, INavigationService
    {
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

                var navigatedViewFunc = default(Func<IView>);
                var navigationFunc = default(Func<Task>);

                if (TabbedPage.GetIndexForViewModel(vmType) is var i and >= 0)
                {
                    navigatedViewFunc = () => TabbedPage.GetTabForIndex(i);
                    navigationFunc = () =>
                    {
                        TabbedPage.SelectedIndex = i;
                        return Task.CompletedTask;
                    };
                }
                else
                {
                    var navigatedPage = CreatePage<T>();
                    navigatedViewFunc = () => navigatedPage;
                    navigationFunc = () => MainPage.PushAsync(navigatedPage, animated);
                }

                bool setQueryProperties(IView navigatedView, IDictionary<string, string> parameters)
                {
                    if (navigatedView is null)
                        return false;
                    if (parameters is null || parameters.Count == 0)
                        return true;

                    var pageType = navigatedView.GetType();
                    var qProps = pageType.GetCustomAttributes(false).OfType<QueryPropertyAttribute>();
                    foreach (var qp in qProps)
                    {
                        if (parameters.TryGetValue(qp.QueryId, out var val) &&
                            pageType.GetProperty(qp.Name) is { CanWrite: true } pi)
                        {
                            pi.SetValue(navigatedView, val);
                        }
                    }

                    return true;
                }

                var queryPropertiesSet = setQueryProperties(navigatedViewFunc(), parameters);

                await navigationFunc();

                if (!queryPropertiesSet)
                    queryPropertiesSet = setQueryProperties(navigatedViewFunc(), parameters);
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