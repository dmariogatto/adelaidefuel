using AdelaideFuel.Droid.Renderers;
using AdelaideFuel.Services;
using AdelaideFuel.UI.Services;
using AdelaideFuel.UI.Views;
using Android.Content;
using Android.Runtime;
using Android.Text;
using Android.Views.InputMethods;
using AndroidX.AppCompat.Widget;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms.Platform.Android.AppCompat;

[assembly: ExportRenderer(typeof(SiteSearchPage), typeof(SearchPageRenderer))]
namespace AdelaideFuel.Droid.Renderers
{
    [Preserve(AllMembers = true)]
    public class SearchPageRenderer : PageRenderer
    {
        private Toolbar _toolbar;
        private SearchView _searchView;

        public SearchPageRenderer(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement?.Parent is NavigationPage navPage &&
                navPage.CurrentPage is ISearchPage &&
                IoC.Resolve<INavigationService>() is TabbedNavigationService)
            {
                AddSearch();
                navPage.Popped += NavigationPagePopped;
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Element is ISearchPage searchPage &&
                GetSearchView() is SearchView searchView)
            {
                if (e.PropertyName == nameof(ISearchPage.Query) &&
                    !string.Equals(searchPage.Query, searchView.Query))
                {
                    searchView.SetQuery(searchPage.Query, false);
                }
                else if (e.PropertyName == nameof(ISearchPage.Placeholder))
                {
                    searchView.QueryHint = searchPage.Placeholder;
                }
            }
        }

        private void AddSearch()
        {
            if (GetToolbar() is Toolbar toolBar &&
                GetSearchView() == null)
            {
                toolBar.InflateMenu(Resource.Menu.SearchMenu);

                if (GetSearchView() is SearchView searchView)
                {
                    searchView.QueryTextChange += HandleQueryTextChange;
                    searchView.ImeOptions = (int)ImeAction.Search;
                    searchView.InputType = (int)InputTypes.TextVariationFilter;
                    searchView.MaxWidth = int.MaxValue;

                    if (Element is ISearchPage searchPage)
                    {
                        searchView.SetQuery(searchPage.Query, false);
                        searchView.QueryHint = searchPage.Placeholder;
                    }
                }
            }
        }

        private void RemoveSearch()
        {
            if (_searchView != null)
            {
                _searchView.QueryTextChange -= HandleQueryTextChange;
                _searchView = null;
            }

            _toolbar?.Menu?.RemoveItem(Resource.Menu.SearchMenu);
            _toolbar = null;
        }

        private void NavigationPagePopped(object sender, NavigationEventArgs e)
        {
            if (sender is NavigationPage navPage && e.Page == Element)
            {
                navPage.Popped -= NavigationPagePopped;
                RemoveSearch();
            }
        }

        private void HandleQueryTextChange(object sender, SearchView.QueryTextChangeEventArgs e)
        {
            if (Element is ISearchPage searchPage)
            {
                searchPage.Query = e.NewText;
            }
        }

        private Toolbar GetToolbar()
        {
            if (_toolbar == null &&
                Element?.Parent is NavigationPage navPage)
            {
                var rendererService = IoC.Resolve<IRendererService>();
                var renderer = rendererService.GetRenderer(navPage);
                if (renderer is NavigationPageRenderer navRenderer)
                {
                    for (var i = 0; i < navRenderer.ChildCount && _toolbar == null; i++)
                    {
                        if (navRenderer.GetChildAt(i) is Toolbar toolbar)
                            _toolbar = toolbar;
                    }
                }
            }

            return _toolbar;
        }

        private SearchView GetSearchView()
        {
            if (_searchView == null)
                _searchView = GetToolbar()?.Menu?.FindItem(Resource.Id.ActionSearch)?.ActionView?.JavaCast<SearchView>();

            return _searchView;
        }
    }
}