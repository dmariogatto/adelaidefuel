using Acr.UserDialogs;
using AdelaideFuel.Services;
using MvvmHelpers.Commands;
using System;
using System.Collections.Generic;
using Xamarin.Essentials;

namespace AdelaideFuel.ViewModels
{
    public abstract class BaseViewModel : MvvmHelpers.BaseViewModel, IViewModel
    {
        protected readonly IFuelService FuelService;
        protected readonly INavigationService NavigationService;
        protected readonly IAppPreferences AppPrefs;
        protected readonly IUserDialogs UserDialogs;
        protected readonly ILogger Logger;

        public BaseViewModel(
            IBvmConstructor bvmConstructor) : base()
        {
            FuelService = bvmConstructor.FuelService;
            NavigationService = bvmConstructor.NavigationService;
            AppPrefs = bvmConstructor.AppPrefs;
            UserDialogs = bvmConstructor.UserDialogs;
            Logger = bvmConstructor.Logger;

            OpenUrlCommand = new AsyncCommand<string>(url => OpenUriCommand.ExecuteAsync(new Uri(url)));
            OpenUriCommand = new AsyncCommand<Uri>(uri =>
                bvmConstructor.Browser.OpenAsync(uri, new BrowserLaunchOptions()
                {
                    PreferredToolbarColor = bvmConstructor.ThemeService.PrimaryColor,
                }));
        }

        public void TrackEvent(string eventName, IDictionary<string, string> properties = null) =>
            Logger.Event(eventName, properties);
        public void TrackEventWithValue(string eventName, object val) =>
            Logger.Event(eventName, new Dictionary<string, string>() { { AppCenterEvents.Property.Value, val.ToString() } });
        public void TrackEventWithOldAndNew(string eventName, object oldVal, object newVal) =>
            Logger.Event(eventName, new Dictionary<string, string>()
                { { AppCenterEvents.Property.Old, oldVal.ToString() }, { AppCenterEvents.Property.New, newVal.ToString() } });

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
    }
}