using AdelaideFuel.Localisation;
using AdelaideFuel.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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

            Themes = Enum.GetNames(typeof(Theme));

            LoadSettingsCommand = new RelayCommand(() =>
            {
                OnPropertyChanged(nameof(LogDataSize));
                OnPropertyChanged(nameof(CacheDataSize));
            });

            SendFeedbackCommand = new AsyncRelayCommand(SendFeedbackAsync);
            RateAppCommand = new AsyncRelayCommand(RateAppAsync);
            OpenAppSettingsCommand = new RelayCommand(_appInfo.ShowSettingsUI);
            GoToSubscriptionCommand = new AsyncRelayCommand(() => NavigationService.NavigateToAsync<SubscriptionViewModel>());
            GoToBrandsCommand = new AsyncRelayCommand(() => NavigationService.NavigateToAsync<BrandsViewModel>());
            GoToFuelsCommand = new AsyncRelayCommand(() => NavigationService.NavigateToAsync<FuelsViewModel>());
            GoToRadiiCommand = new AsyncRelayCommand(() => NavigationService.NavigateToAsync<RadiiViewModel>());
            BuildTappedCommand = new AsyncRelayCommand(BuildTappedAsync);
            GenerateTestCrashCommand = new RelayCommand(() =>
            {
                try { throw new Exception("Test Crash"); }
                catch (Exception ex) { Logger.Error(ex); }
            });
            ViewLogCommand = new AsyncRelayCommand(ViewLogAsync);
            DeleteLogCommand = new RelayCommand(DeleteLog);
            ClearCacheCommand = new RelayCommand(ClearCache);
        }

        #region Overrides
        public override void OnAppearing()
        {
            base.OnAppearing();

            LoadSettingsCommand.Execute(null);

            TrackEvent(Events.PageView.SettingsView);
        }

        public override void OnDisappearing()
        {
            base.OnDisappearing();

            BuildTappedCount = 0;
        }
        #endregion

        public int CurrentYear => DateTime.Today.Year;

        public string Version => _appInfo.VersionString;
        public string Build => _appInfo.BuildString;

        private int _buildTappedCount;
        public int BuildTappedCount
        {
            get => _buildTappedCount;
            set => SetProperty(ref _buildTappedCount, value);
        }

        public long LogDataSize => Logger.LogInBytes();
        public long CacheDataSize => _storeFactory.CacheSizeInBytes();

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

                    TrackEventWithOldAndNew(Events.Setting.AppTheme, oldVal, value);
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

        public IReadOnlyList<string> Themes { get; private set; } = [];

        public RelayCommand LoadSettingsCommand { get; private set; }
        public AsyncRelayCommand SendFeedbackCommand { get; private set; }
        public AsyncRelayCommand RateAppCommand { get; private set; }
        public RelayCommand OpenAppSettingsCommand { get; private set; }
        public AsyncRelayCommand GoToSubscriptionCommand { get; private set; }
        public AsyncRelayCommand GoToBrandsCommand { get; private set; }
        public AsyncRelayCommand GoToFuelsCommand { get; private set; }
        public AsyncRelayCommand GoToRadiiCommand { get; private set; }
        public AsyncRelayCommand BuildTappedCommand { get; private set; }
        public RelayCommand GenerateTestCrashCommand { get; private set; }
        public AsyncRelayCommand ViewLogCommand { get; private set; }
        public RelayCommand DeleteLogCommand { get; private set; }
        public RelayCommand ClearCacheCommand { get; private set; }

        private async Task SendFeedbackAsync()
        {
            try
            {
                var message = _email.GetFeedbackEmailMessage(_appInfo, _deviceInfo);
                await _email.ComposeAsync(message);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);

                DialogService.Alert(Resources.EmailDirectly, Resources.UnableToSendEmail, Resources.OK);
            }
        }

        private async Task RateAppAsync()
        {
            var id = Constants.AppId;
            if (!string.IsNullOrEmpty(id))
                await _storeReview.OpenStoreReviewPageAsync(id);
        }

        private async Task BuildTappedAsync()
        {
            try
            {
                BuildTappedCount++;

                if (BuildTappedCount == 5)
                {
                    var message = _email.GetFeedbackEmailMessage(_appInfo, _deviceInfo);

                    if (Logger.LogFilePath() is string logFilePath && File.Exists(logFilePath))
                    {
                        message.Attachments = new List<EmailAttachment>()
                        {
                            new EmailAttachment(logFilePath, "text/plain")
                        };
                    }

                    await _email.ComposeAsync(message);

                    BuildTappedCount = 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private async Task ViewLogAsync()
        {
            var log = await Logger.GetLogAsync();
            if (!string.IsNullOrEmpty(log))
            {
                await _clipboard.SetTextAsync(log);
                await DialogService.AlertAsync(log, "Log", Resources.OK);
            }
            else
            {
                await DialogService.AlertAsync("EMPTY 👍", "Log", Resources.OK);
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