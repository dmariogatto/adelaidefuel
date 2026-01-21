using AdelaideFuel.Maui.Converters;
using AdelaideFuel.Services;
using Cats.Maui.AdMob;
using System.ComponentModel;

namespace AdelaideFuel.Maui.Controls
{
    [ContentProperty(nameof(Content))]
    public class AdContentView : Grid
    {
        private readonly IAdConsentService _adConsentService;
        private readonly ISubscriptionService _subscriptionService;

        private readonly RowDefinition _adRowDefinition;
        private readonly AdSmartBanner _adBannerView;
        private readonly SkeletonView _adSkeleton;

        private bool _resumedRegistered = false;

        public AdContentView() : base()
        {
            _adConsentService = IoC.Resolve<IAdConsentService>();
            _subscriptionService = IoC.Resolve<ISubscriptionService>();

            SafeAreaEdges = SafeAreaEdges.None;

            _adRowDefinition = new RowDefinition(0);
            _adBannerView = new AdSmartBanner();
            _adSkeleton = new SkeletonView()
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions =  LayoutOptions.Fill,
            };

            RowDefinitions.Add(new RowDefinition(GridLength.Star));
            RowDefinitions.Add(_adRowDefinition);

            var equalityConverter = new EqualityConverter();
            var inverseEqualityConverter = new InverseEqualityConverter();

            _adBannerView.SetBinding(
                IsVisibleProperty,
                static (AdSmartBanner i) => i.AdStatus,
                converter: inverseEqualityConverter,
                converterParameter: AdLoadStatus.Failed,
                mode: BindingMode.OneWay,
                source: _adBannerView);
            _adSkeleton.SetBinding(
                IsVisibleProperty,
                static (AdSmartBanner i) => i.AdStatus,
                converter: inverseEqualityConverter,
                converterParameter: AdLoadStatus.Loaded,
                mode: BindingMode.OneWay,
                source: _adBannerView);
            _adSkeleton.SetBinding(
                SkeletonView.IsAnimatingProperty,
                static (AdSmartBanner i) => i.AdStatus,
                converter: equalityConverter,
                converterParameter: AdLoadStatus.Loading,
                mode: BindingMode.OneWay,
                source: _adBannerView);
        }

        public View Content
        {
            get;
            set
            {
                if (field is not null)
                    Children.Remove(field);

                field = value;

                if (field is not null)
                    this.Add(field, 0, 0);
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
            _adBannerView.PropertyChanged += AdBannerViewOnPropertyChanged;
            AddRemoveBannerAd();
        }

        public void OnDisappearing()
        {
            _adConsentService.AdConsentStatusChanged -= AdConsentStatusChanged;
            _adBannerView.PropertyChanged -= AdBannerViewOnPropertyChanged;
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

            SetAdBannerRowHeight();
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

        private void SetAdBannerRowHeight()
        {
            if (_adBannerView.Parent is null || _adBannerView.AdStatus == AdLoadStatus.Failed)
            {
                _adRowDefinition.Height = 0;
            }
            else
            {
                _adRowDefinition.Height = Math.Max(0, _adBannerView.HeightRequest = _adBannerView.AdSizeRequest.Height);
            }
        }

        private void AdBannerViewOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AdSmartBanner.AdStatus))
            {
                SetAdBannerRowHeight();
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