using AdelaideFuel.Localisation;
using AdelaideFuel.Services;
using Microsoft.AppCenter.Crashes;
using MvvmHelpers;
using MvvmHelpers.Commands;
using Plugin.StoreReview.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace AdelaideFuel.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly IDeviceInfo _deviceInfo;
        private readonly IAppInfo _appInfo;
        private readonly IEmail _email;
        private readonly IClipboard _clipboard;

        private readonly IStoreReview _storeReview;

        private readonly IStoreFactory _storeFactory;
        private readonly IThemeService _themeService;

        public SettingsViewModel(
            IDeviceInfo deviceInfo,
            IAppInfo appInfo,
            IEmail email,
            IClipboard clipboard,
            IStoreReview storeReview,
            IStoreFactory storeFactory,
            IThemeService themeService,
            IBvmConstructor bvmConstructor) : base(bvmConstructor)
        {
            Title = Resources.Settings;

            _deviceInfo = deviceInfo;
            _appInfo = appInfo;
            _email = email;
            _clipboard = clipboard;
            _storeReview = storeReview;

            _storeFactory = storeFactory;
            _themeService = themeService;

            Themes = new ObservableRangeCollection<string>(Enum.GetNames(typeof(Theme)));

            LoadSettingsCommand = new Command(() =>
            {
                OnPropertyChanged(nameof(LogDataSize));
                OnPropertyChanged(nameof(CacheDataSize));
                OnPropertyChanged(nameof(UserDataSize));
            });

            SendFeedbackCommand = new AsyncCommand(SendFeedbackAsync);
            RateAppCommand = new Command(RateApp);
            OpenAppSettingsCommand = new Command(_appInfo.ShowSettingsUI);
            GoToSubscriptionCommand = new AsyncCommand(() => NavigationService.NavigateToAsync<SubscriptionViewModel>());
            GoToBrandsCommand = new AsyncCommand(() => NavigationService.NavigateToAsync<BrandsViewModel>());
            GoToFuelsCommand = new AsyncCommand(() => NavigationService.NavigateToAsync<FuelsViewModel>());
            GoToRadiiCommand = new AsyncCommand(() => NavigationService.NavigateToAsync<RadiiViewModel>());
            GenerateTestCrashCommand = new Command(() =>
            {
                try { Crashes.GenerateTestCrash(); }
                catch (Exception ex) { Logger.Error(ex); }
            });
            ViewLogCommand = new AsyncCommand(ViewLogAsync);
            DeleteLogCommand = new Command(DeleteLog);
            ClearCacheCommand = new Command(ClearCache);
        }

        #region Overrides
        public override void OnAppearing()
        {
            base.OnAppearing();

            LoadSettingsCommand.Execute(null);

            TrackEvent(AppCenterEvents.PageView.SettingsView);
        }
        #endregion

        public string Version => _appInfo.VersionString;
        public string Build => _appInfo.BuildString;

        public long LogDataSize => Logger.LogInBytes();
        public long CacheDataSize => _storeFactory.CacheSizeInBytes();
        public long UserDataSize => _storeFactory.UserSizeInBytes();

        public Theme AppTheme
        {
            get => AppPrefs.AppTheme;
            set
            {
                var oldVal = AppTheme;
                if (oldVal != value)
                {
                    _themeService.SetTheme(value);

                    OnPropertyChanged(nameof(AppTheme));

                    TrackEventWithOldAndNew(AppCenterEvents.Setting.AppTheme, oldVal, value);
                }
            }
        }

        public bool ShowRadiiOnMap
        {
            get => AppPrefs.ShowRadiiOnMap;
            set
            {
                if (ShowRadiiOnMap != value)
                {
                    AppPrefs.ShowRadiiOnMap = value;
                    OnPropertyChanged(nameof(ShowRadiiOnMap));
                }
            }
        }

        public ObservableRangeCollection<string> Themes { get; private set; }

        public Command LoadSettingsCommand { get; private set; }
        public AsyncCommand SendFeedbackCommand { get; private set; }
        public Command RateAppCommand { get; private set; }
        public Command OpenAppSettingsCommand { get; private set; }
        public AsyncCommand GoToSubscriptionCommand { get; private set; }
        public AsyncCommand GoToBrandsCommand { get; private set; }
        public AsyncCommand GoToFuelsCommand { get; private set; }
        public AsyncCommand GoToRadiiCommand { get; private set; }
        public Command GenerateTestCrashCommand { get; private set; }
        public AsyncCommand ViewLogCommand { get; private set; }
        public Command DeleteLogCommand { get; private set; }
        public Command ClearCacheCommand { get; private set; }

        private async Task SendFeedbackAsync()
        {
            try
            {
                var builder = new StringBuilder();
                builder.AppendLine($"App: {_appInfo.VersionString} | {_appInfo.BuildString}");
                builder.AppendLine($"OS: {_deviceInfo.Platform} | {_deviceInfo.VersionString}");
                builder.AppendLine($"Device: {_deviceInfo.Manufacturer} | {_deviceInfo.Model}");
                builder.AppendLine();
                builder.AppendLine(string.Format(Resources.ItemComma, Resources.AddYourMessageBelow));
                builder.AppendLine("----");
                builder.AppendLine();

                var message = new EmailMessage
                {
                    Subject = string.Format(Resources.FeedbackSubjectItem, _deviceInfo.Platform),
                    Body = builder.ToString(),
                    To = new List<string>(1) { Constants.Email },
                };

                await _email.ComposeAsync(message);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);

                UserDialogs.Alert(Resources.EmailDirectly, Resources.UnableToSendEmail, Resources.OK);
            }
        }

        private void RateApp()
        {
            var id = Constants.AppId;

            if (!string.IsNullOrEmpty(id))
                _storeReview.OpenStoreReviewPage(id);
        }

        private async Task ViewLogAsync()
        {
            var log = await Logger.GetLog();
            if (!string.IsNullOrEmpty(log))
            {
                await _clipboard.SetTextAsync(log);
                await UserDialogs.AlertAsync(log, "Log", Resources.OK);
            }
            else
            {
                await UserDialogs.AlertAsync("EMPTY 👍", "Log", Resources.OK);
            }
        }

        private void DeleteLog()
        {
            Logger.DeleteLog();
            LoadSettingsCommand.Execute(null);
        }

        private void ClearCache()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                _storeFactory.CacheEmptyAll();
                _storeFactory.CacheCheckpoint();
                _storeFactory.CacheRebuild();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                IsBusy = false;
            }

            LoadSettingsCommand.Execute(null);
        }
    }
}