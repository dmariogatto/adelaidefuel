using AdelaideFuel.Maui.Converters;
using AdelaideFuel.Services;
using Cats.Maui.AdMob;

namespace AdelaideFuel.Maui.Controls
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

            RowDefinitions.Add(new RowDefinition(GridLength.Star));
            RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        public View Content
        {
            get => _mainView;
            set
            {
                if (_mainView is not null)
                    Remove(_mainView);

                _mainView = value;

                if (_mainView is not null)
                    this.Add(_mainView, 0, 0);
            }
        }

        public string AdUnitId
        {
            get => _adBannerContainer.AdUnitId;
            set
            {
                _adBannerContainer.AdUnitId = value;
                AddRemoveBannerAd();
            }
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
            if (_subscriptionService.AdsEnabled && _adConsentService.CanServeAds && !string.IsNullOrEmpty(AdUnitId))
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
            if (!Children.Contains(_adBannerContainer))
            {
                this.Add(_adBannerContainer, 0, 1);
            }
        }

        private void RemoveBannerAd()
            => Children.Remove(_adBannerContainer);

        private class AdBannerContainer : Grid
        {
            private readonly AdSmartBanner _adBannerView;

            public AdBannerContainer() : base()
            {
                HorizontalOptions = LayoutOptions.Fill;
                VerticalOptions = LayoutOptions.Start;

                _adBannerView = new AdSmartBanner() { HeightRequest = 0 };

                var adSkeleton = new SkeletonView();
                adSkeleton.HorizontalOptions = adSkeleton.VerticalOptions = LayoutOptions.Fill;

                var mainRowDefinition = new RowDefinition();

                mainRowDefinition.SetBinding(
                    RowDefinition.HeightProperty,
                    static (AdSmartBanner i) => i.HeightRequest,
                    mode: BindingMode.OneWay,
                    source: _adBannerView);

                RowDefinitions.Add(mainRowDefinition);

                Children.Add(adSkeleton);
                Children.Add(_adBannerView);

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