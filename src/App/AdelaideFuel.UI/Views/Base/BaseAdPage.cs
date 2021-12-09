using AdelaideFuel.Services;
using AdelaideFuel.UI.Controls;
using AdelaideFuel.UI.Converters;
using AdelaideFuel.ViewModels;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Views
{
    [ContentProperty(nameof(MainContent))]
    public class BaseAdPage<T> : BasePage<T> where T : BaseViewModel
    {
        private readonly Grid _mainGrid;
        private readonly AdSmartBanner _adBannerView;

        private View _mainView;

        public BaseAdPage() : base()
        {
            _mainGrid = new Grid() { RowSpacing = 0 };

            _mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Star });

            var adRowDefinition = new RowDefinition() { Height = 0 };
            _mainGrid.RowDefinitions.Add(adRowDefinition);

            _adBannerView = new AdSmartBanner() { HeightRequest = 0 };

            var bannerContainer = new Grid();

            var background = new ContentView();
            background.SetDynamicResource(BackgroundColorProperty, Styles.Keys.PageBackgroundColor);

            var adSkeleton = new SkeletonView();

            bannerContainer.Children.Add(background);
            bannerContainer.Children.Add(adSkeleton);
            bannerContainer.Children.Add(_adBannerView);

            _mainGrid.Children.Add(bannerContainer, 0, 1);

            adSkeleton.SetBinding(IsVisibleProperty,
                new Binding(nameof(AdSmartBanner.AdStatus),
                            converter: new AdNotLoadedConverter(),
                            source: _adBannerView));
            adRowDefinition.SetBinding(RowDefinition.HeightProperty,
                new Binding(nameof(HeightRequest),
                            converter: new DoubleToGridLengthConverter(),
                            source: _adBannerView));

            Content = _mainGrid;
        }

        public View MainContent
        {
            get => _mainView;
            set
            {
                if (_mainView != null)
                    _mainGrid.Children.Remove(_mainView);

                _mainView = value;

                if (_mainView != null)
                    _mainGrid.Children.Insert(0, _mainView);
            }
        }

        public string AdUnitId
        {
            get => _adBannerView?.AdUnitId ?? string.Empty;
            set
            {
                if (_adBannerView != null)
                    _adBannerView.AdUnitId = value;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }
    }
}