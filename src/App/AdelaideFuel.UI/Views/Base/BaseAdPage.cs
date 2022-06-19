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
        private readonly ISubscriptionService _subscriptionService;

        private readonly Grid _mainGrid;
        private readonly AdSmartBanner _adBannerView;

        private View _mainView;

        public BaseAdPage() : base()
        {
            _subscriptionService = IoC.Resolve<ISubscriptionService>();

            _mainGrid = new Grid() { RowSpacing = 0 };
            _mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Star });

            _adBannerView = new AdSmartBanner() { HeightRequest = 0 };

            var bannerContainer = new Grid();
            var boxView = new BoxView();
            var adSkeleton = new SkeletonView();

            bannerContainer.Children.Add(boxView);
            bannerContainer.Children.Add(adSkeleton);
            bannerContainer.Children.Add(_adBannerView);

            boxView.SetDynamicResource(BoxView.BackgroundColorProperty, Styles.Keys.PageBackgroundColor);
            boxView.HorizontalOptions = boxView.VerticalOptions = LayoutOptions.Fill;

            boxView.SetBinding(HeightRequestProperty,
                new Binding(nameof(HeightRequest), source: adSkeleton));
            boxView.SetBinding(IsVisibleProperty,
                new Binding(nameof(IsVisible), source: adSkeleton));
            adSkeleton.SetBinding(HeightRequestProperty,
                new Binding(nameof(HeightRequest), source: _adBannerView));
            adSkeleton.SetBinding(IsVisibleProperty,
                new Binding(nameof(AdSmartBanner.AdStatus),
                            converter: new AdNotLoadedConverter(),
                            source: _adBannerView));

            if (_subscriptionService.AdsEnabled)
            {
                AddBannerAd();
            }

            Content = _mainGrid;
        }

        public View MainContent
        {
            get => _mainView;
            set
            {
                if (_mainView is not null)
                    _mainGrid.Children.Remove(_mainView);

                _mainView = value;

                if (_mainView is not null)
                    _mainGrid.Children.Insert(0, _mainView);
            }
        }

        public string AdUnitId
        {
            get => _adBannerView?.AdUnitId ?? string.Empty;
            set
            {
                if (_adBannerView is not null)
                    _adBannerView.AdUnitId = value;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (_subscriptionService.AdsEnabled)
            {
                AddBannerAd();
            }
            else
            {
                RemoveBannerAd();
            }
        }

        private void AddBannerAd()
        {
            if (_adBannerView?.Parent is View container && container.Parent is null)
            {
                _mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                _mainGrid.Children.Add(container, 0, 1);
            }
        }

        private void RemoveBannerAd()
        {
            if (_adBannerView?.Parent is View container && ReferenceEquals(container.Parent, _mainGrid))
            {
                _mainGrid.Children.Remove(container);
                _mainGrid.RowDefinitions.RemoveAt(_mainGrid.RowDefinitions.Count - 1);
            }
        }
    }
}