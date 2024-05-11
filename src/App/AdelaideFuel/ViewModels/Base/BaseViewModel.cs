using Acr.UserDialogs;
using AdelaideFuel.Services;
using Microsoft.Maui.ApplicationModel;
using MvvmHelpers.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdelaideFuel.ViewModels
{
    public abstract class BaseViewModel : MvvmHelpers.BaseViewModel, IViewModel
    {
        protected readonly IFuelService FuelService;
        protected readonly INavigationService NavigationService;
        protected readonly IAppPreferences AppPrefs;
        protected readonly IUserDialogs UserDialogs;
        protected readonly ILogger Logger;

        private readonly IBrowser _browser;
        private readonly IThemeService _themeService;

        public BaseViewModel(
            IBvmConstructor bvmConstructor) : base()
        {
            FuelService = bvmConstructor.FuelService;
            NavigationService = bvmConstructor.NavigationService;
            AppPrefs = bvmConstructor.AppPrefs;
            UserDialogs = bvmConstructor.UserDialogs;
            Logger = bvmConstructor.Logger;

            _browser = bvmConstructor.Browser;
            _themeService = bvmConstructor.ThemeService;

            OpenUrlCommand = new AsyncCommand<string>(OpenUrlAsync);
            OpenUriCommand = new AsyncCommand<Uri>(OpenUriAsync);
        }

        public void TrackEvent(string eventName, IReadOnlyDictionary<string, string> properties = null) =>
            Logger.Event(eventName, properties);
        public void TrackEventWithValue(string eventName, object val) =>
            Logger.Event(eventName, new Dictionary<string, string>() { { Events.Property.Value, val.ToString() } });
        public void TrackEventWithOldAndNew(string eventName, object oldVal, object newVal) =>
            Logger.Event(eventName, new Dictionary<string, string>()
                { { Events.Property.Old, oldVal.ToString() }, { Events.Property.New, newVal.ToString() } });

        public AsyncCommand<string> OpenUrlCommand { get; private set; }
        public AsyncCommand<Uri> OpenUriCommand { get; private set; }

        public virtual void OnCreate()
        {
        }

        public virtual void OnAppearing()
        {
        }

        public virtual void OnDisappearing()
        {
        }

        public virtual void OnDestory()
        {
        }

#if DEBUG
        private bool _isDevelopment = true;
#else
        private bool _isDevelopment = false;
#endif
        public bool IsDevelopment
        {
            get => _isDevelopment;
            set => OnPropertyChanged(nameof(IsDevelopment));
        }

        private bool _hasError;
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        private async Task OpenUrlAsync(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                await OpenUriAsync(uri);
        }

        private async Task OpenUriAsync(Uri uri)
        {
            if (uri is null)
                return;

            try
            {
                await _browser.OpenAsync(uri, new BrowserLaunchOptions()
                {
                    PreferredToolbarColor = _themeService.PrimaryColor.ToMaui(),
                    PreferredControlColor = _themeService.ContrastColor.ToMaui(),
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);

                UserDialogs.Alert(
                    string.Format(Localisation.Resources.IssueOpeningUrlItem, uri.ToString()),
                    Localisation.Resources.Error,
                    Localisation.Resources.OK);
            }
        }
    }
}