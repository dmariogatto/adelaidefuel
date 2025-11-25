using AdelaideFuel.Maui.Controls;
using AdelaideFuel.Maui.Converters;
using AdelaideFuel.Services;
using Cats.Maui.AdMob;

namespace AdelaideFuel.Maui.Views
{
    [ContentProperty(nameof(Content))]
    public class AdContentView : Grid
    {
        private readonly IAdConsentService _adConsentService;
        private readonly ISubscriptionService _subscriptionService;

        private readonly AdBannerContainer _adBannerContainer;

        private View _mainView;

        public AdContentView() : base()
        {
            _adConsentService = IoC.Resolve<IAdConsentService>();
            _subscriptionService = IoC.Resolve<ISubscriptionService>();

            _adBannerContainer = new AdBannerContainer();

            SafeAreaEdges = SafeAreaEdges.None;

            RowDefinitions.Add(new RowDefinition() { Height = GridLength.Star });
        }

        public View Content
        {
            get => _mainView;
            set
            {
                if (_mainView is not null)
                    Children.Remove(_mainView);

                _mainView = value;

                if (_mainView is not null)
                    Children.Insert(0, _mainView);
            }
        }

        public string AdUnitId
        {
            get => _adBannerContainer.AdUnitId;
            set => _adBannerContainer.AdUnitId = value;
        }

        public void OnAppearing()
        {
            _adConsentService.AdConsentStatusChanged += AdConsentStatusChanged;
            AddRemoveBannerAd();
        }

        public void OnDisappearing()
        {
            _adConsentService.AdConsentStatusChanged -= AdConsentStatusChanged;
        }

        private void AdConsentStatusChanged(object sender, AdConsentStatusChangedEventArgs e)
            => AddRemoveBannerAd();

        private void AddRemoveBannerAd()
        {
            if (_subscriptionService.AdsEnabled && _adConsentService.CanServeAds)
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
            if (_adBannerContainer.Parent is null)
            {
                RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                this.Add(_adBannerContainer, 0, 1);
            }
        }

        private void RemoveBannerAd()
        {
            if (ReferenceEquals(_adBannerContainer.Parent, this))
            {
                Children.Remove(_adBannerContainer);
                RowDefinitions.RemoveAt(RowDefinitions.Count - 1);
            }
        }

        private class AdBannerContainer : Grid
        {
            private readonly AdSmartBanner _adBannerView;

            public AdBannerContainer() : base()
            {
                _adBannerView = new AdSmartBanner() { HeightRequest = 0 };

                var boxView = new BoxView();
                var adSkeleton = new SkeletonView();

                Children.Add(boxView);
                Children.Add(adSkeleton);
                Children.Add(_adBannerView);

                boxView.SetDynamicResource(BoxView.ColorProperty, Styles.Keys.PageBackgroundColor);
                boxView.HorizontalOptions = boxView.VerticalOptions = LayoutOptions.Fill;

                boxView.SetBinding(
                    HeightRequestProperty,
                    static (AdSmartBanner i) => i.HeightRequest,
                    mode: BindingMode.OneWay,
                    source: _adBannerView);
                boxView.SetBinding(
                    IsVisibleProperty,
                    static (AdSmartBanner i) => i.IsVisible,
                    mode: BindingMode.OneWay,
                    source: _adBannerView);

                adSkeleton.SetBinding(
                    HeightRequestProperty,
                    static (AdSmartBanner i) => i.HeightRequest,
                    mode: BindingMode.OneWay,
                    source: _adBannerView);
                adSkeleton.SetBinding(
                    IsVisibleProperty,
                    static (AdSmartBanner i) => i.AdStatus,
                    converter: new InverseEqualityConverter(),
                    converterParameter: AdLoadStatus.Loaded,
                    mode: BindingMode.OneWay,
                    source: _adBannerView);
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
        }
    }
}