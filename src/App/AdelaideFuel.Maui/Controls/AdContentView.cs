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

        private readonly AdSmartBanner _adBannerView;
        private readonly SkeletonView _adSkeleton;

        private View _mainView;
        private bool _resumedRegistered = false;

        public AdContentView() : base()
        {
            _adConsentService = IoC.Resolve<IAdConsentService>();
            _subscriptionService = IoC.Resolve<ISubscriptionService>();

            SafeAreaEdges = SafeAreaEdges.None;

            _adBannerView = new AdSmartBanner() { HeightRequest = 0 };

            _adSkeleton = new SkeletonView()
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions =  LayoutOptions.Fill,
            };

            RowDefinitions.Add(new RowDefinition(GridLength.Star));
            RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            _adSkeleton.SetBinding(
                IsVisibleProperty,
                static (AdSmartBanner i) => i.AdStatus,
                converter: new InverseEqualityConverter(),
                converterParameter: AdLoadStatus.Loaded,
                mode: BindingMode.OneWay,
                source: _adBannerView);
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
                    this.Add(_mainView, 0, 0);
            }
        }

        public string AdUnitId
        {
            get => _adBannerView?.AdUnitId ?? string.Empty;
            set => _adBannerView?.AdUnitId = value;
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

        private void OnAppResumed(object sender, EventArgs e)
        {
            if (_adBannerView.Parent is not null)
            {
                _adBannerView.Load();
            }
        }

        private void AddBannerAd()
        {
            if (_adBannerView.Parent is null)
            {
                this.Add(_adSkeleton, 0, 1);
                this.Add(_adBannerView, 0, 1);
            }
            else if (DateTime.UtcNow - _adBannerView.AdLoadedDateUtc > TimeSpan.FromSeconds(60))
            {
                _adBannerView.Load();
            }
        }

        private void RemoveBannerAd()
        {
            if (ReferenceEquals(_adBannerView.Parent, this))
            {
                Children.Remove(_adSkeleton);
                Children.Remove(_adBannerView);
            }
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            if (Handler is not null && !_resumedRegistered)
            {
                (Application.Current as App)?.Resumed += OnAppResumed;
                _resumedRegistered = true;
            }
            else if (_resumedRegistered)
            {
                (Application.Current as App)?.Resumed -= OnAppResumed;
                _resumedRegistered = false;
            }
        }
    }
}