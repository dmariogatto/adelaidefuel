﻿using AdelaideFuel.Maui.Controls;
using AdelaideFuel.Maui.Converters;
using AdelaideFuel.Services;
using AdelaideFuel.ViewModels;
using Cats.Maui.AdMob;

namespace AdelaideFuel.Maui.Views
{
    [ContentProperty(nameof(MainContent))]
    public class BaseAdPage<T> : BasePage<T> where T : BaseViewModel
    {
        private readonly IAdConsentService _adConsentService;
        private readonly ISubscriptionService _subscriptionService;

        private readonly Grid _mainGrid;
        private readonly AdSmartBanner _adBannerView;

        private View _mainView;

        public BaseAdPage() : base()
        {
            _adConsentService = IoC.Resolve<IAdConsentService>();
            _subscriptionService = IoC.Resolve<ISubscriptionService>();

            _mainGrid = new Grid() { IgnoreSafeArea = true };
            _mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Star });

            _adBannerView = new AdSmartBanner() { HeightRequest = 0 };

            var bannerContainer = new Grid();
            var boxView = new BoxView();
            var adSkeleton = new SkeletonView();

            bannerContainer.Children.Add(boxView);
            bannerContainer.Children.Add(adSkeleton);
            bannerContainer.Children.Add(_adBannerView);

            boxView.SetDynamicResource(BoxView.ColorProperty, Styles.Keys.PageBackgroundColor);
            boxView.HorizontalOptions = boxView.VerticalOptions = LayoutOptions.Fill;

            boxView.SetBinding(HeightRequestProperty,
                new Binding(nameof(HeightRequest), source: _adBannerView));
            boxView.SetBinding(IsVisibleProperty,
                new Binding(nameof(IsVisible), source: _adBannerView));

            adSkeleton.SetBinding(HeightRequestProperty,
                new Binding(nameof(HeightRequest), source: _adBannerView));
            adSkeleton.SetBinding(IsVisibleProperty,
                new Binding(nameof(AdSmartBanner.AdStatus),
                            converter: new InverseEqualityConverter(),
                            converterParameter: AdLoadStatus.Loaded,
                            source: _adBannerView));

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

            _adConsentService.AdConsentStatusChanged += AdConsentStatusChanged;
            AddRemoveBannerAd();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

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
            if (_adBannerView?.Parent is View container && container.Parent is null)
            {
                _mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                _mainGrid.Add(container, 0, 1);
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