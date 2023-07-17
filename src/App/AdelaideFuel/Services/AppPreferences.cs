using MvvmHelpers;
using System;
using Xamarin.Essentials.Interfaces;

namespace AdelaideFuel.Services
{
    public class AppPreferences : ObservableObject, IAppPreferences
    {
        private readonly IAppClock _clock;
        private readonly IPreferences _preferences;

        public AppPreferences(
            IAppClock clock,
            IPreferences preferences)
        {
            _clock = clock;
            _preferences = preferences;
        }

        public Theme AppTheme
        {
            get => (Theme)_preferences.Get(nameof(AppTheme), (int)Theme.System);
            set
            {
                if (AppTheme != value)
                {
                    _preferences.Set(nameof(AppTheme), (int)value);
                    OnPropertyChanged();
                }
            }
        }

        public DateTime LastDateSynced
        {
            get => _preferences.Get(nameof(LastDateSynced), DateTime.MinValue);
            set
            {
                if (LastDateSynced != value.Date)
                {
                    _preferences.Set(nameof(LastDateSynced), value.Date);
                    OnPropertyChanged();
                }
            }
        }

        public DateTime LastDateOpened
        {
            get => _preferences.Get(nameof(LastDateOpened), _clock.Today.AddDays(-1));
            set
            {
                if (LastDateOpened != value.Date)
                {
                    _preferences.Set(nameof(LastDateOpened), value.Date);
                    OnPropertyChanged();
                }
            }
        }

        public int DayCount
        {
            get => _preferences.Get(nameof(DayCount), 0);
            set
            {
                if (DayCount != value)
                {
                    _preferences.Set(nameof(DayCount), value);
                    OnPropertyChanged();
                }
            }
        }

        public bool ReviewRequested
        {
            get => _preferences.Get(nameof(ReviewRequested), false);
            set
            {
                if (ReviewRequested != value)
                {
                    _preferences.Set(nameof(ReviewRequested), value);
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowRadiiOnMap
        {
#if DEBUG
            get => _preferences.Get(nameof(ShowRadiiOnMap), false);
#else
            get => false;
#endif
            set
            {
                if (ShowRadiiOnMap != value)
                {
                    _preferences.Set(nameof(ShowRadiiOnMap), value);
                    OnPropertyChanged();
                }
            }
        }
    }
}