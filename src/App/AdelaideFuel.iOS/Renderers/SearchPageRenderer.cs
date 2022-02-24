using AdelaideFuel.iOS.Renderers;
using AdelaideFuel.Services;
using AdelaideFuel.UI.Services;
using AdelaideFuel.UI.Views;
using Foundation;
using System;
using System.ComponentModel;
using UIKit;
using Xamarin.Forms;

[assembly: ExportRenderer(typeof(SiteSearchPage), typeof(SearchPageRenderer))]
namespace AdelaideFuel.iOS.Renderers
{
    [Preserve(AllMembers = true)]
    public class SearchPageRenderer : CustomPageRenderer, IUISearchResultsUpdating
    {
        private static readonly Lazy<INavigationService> NavigationService = new Lazy<INavigationService>(() => IoC.Resolve<INavigationService>());

        private UINavigationItemLargeTitleDisplayMode _largeTitleDisplayMode;
        private UISearchController _searchController;

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            if (NavigationService.Value is TabbedNavigationService && _searchController == null)
            {
                // Non-shell

                _searchController = new UISearchController(searchResultsController: null)
                {
                    SearchResultsUpdater = this,
                    DimsBackgroundDuringPresentation = false,
                    HidesNavigationBarDuringPresentation = true,
                    HidesBottomBarWhenPushed = true
                };

                var searchBar = _searchController.SearchBar;

                searchBar.ShowsCancelButton = false;
                searchBar.TextChanged += delegate { searchBar.SetShowsCancelButton(true, true); };
                searchBar.OnEditingStarted += delegate { searchBar.SetShowsCancelButton(true, true); };
                searchBar.OnEditingStopped += delegate { searchBar.SetShowsCancelButton(false, true); };

                WirePropertyChanged();

                if (_searchController != null && ParentViewController.NavigationItem.SearchController is null)
                {
                    if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
                    {
                        _largeTitleDisplayMode = ParentViewController.NavigationItem.LargeTitleDisplayMode;
                        ParentViewController.NavigationItem.LargeTitleDisplayMode = UINavigationItemLargeTitleDisplayMode.Automatic;
                        ParentViewController.NavigationItem.HidesSearchBarWhenScrolling = false;
                    }

                    ParentViewController.NavigationItem.SearchController = _searchController;
                    DefinesPresentationContext = true;
                }
            }
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
            {
                ParentViewController.NavigationItem.LargeTitleDisplayMode = _largeTitleDisplayMode;
            }

            ParentViewController.NavigationItem.SearchController = null;
            UnWirePropertyChanged();
        }

        public void UpdateSearchResultsForSearchController(UISearchController searchController)
        {
            if (Element is ISearchPage searchPage)
            {
                searchPage.Query = _searchController?.SearchBar?.Text ?? string.Empty;
            }
        }

        private void WirePropertyChanged()
        {
            if (Element is ISearchPage searchPage)
            {
                if (_searchController != null)
                {
                    _searchController.SearchBar.Placeholder = searchPage.Placeholder;
                    _searchController.SearchBar.Text = searchPage.Query;
                }

                searchPage.PropertyChanged += SearchPagePropertyChanged;
            }
        }

        private void UnWirePropertyChanged()
        {
            if (Element is ISearchPage searchPage)
            {
                searchPage.PropertyChanged -= SearchPagePropertyChanged;
            }
        }

        private void SearchPagePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_searchController != null && Element is ISearchPage searchPage)
            {
                if (e.PropertyName == nameof(ISearchPage.Query) &&
                    !string.Equals(_searchController.SearchBar.Text, searchPage.Query))
                {
                    _searchController.SearchBar.Text = searchPage.Query;
                }
                else if (e.PropertyName == nameof(ISearchPage.Placeholder))
                {
                    _searchController.SearchBar.Placeholder = searchPage.Placeholder;
                }
            }
        }
    }
}