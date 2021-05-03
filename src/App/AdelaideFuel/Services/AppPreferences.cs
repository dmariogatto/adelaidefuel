using MvvmHelpers;
using System;
using Xamarin.Essentials.Interfaces;

namespace AdelaideFuel.Services
{
    public class AppPreferences : ObservableObject, IAppPreferences
    {
        private readonly IPreferences _preferences;

        public AppPreferences(IPreferences preferences)
        {
            _preferences = preferences;
        }

        public Theme AppTheme
        {
            get => (Theme)_preferences.Get(nameof(AppTheme), (int)Theme.System);
            set
            {
                var oldVal = AppTheme;

                _preferences.Set(nameof(AppTheme), (int)value);

                if (oldVal != AppTheme)
                    OnPropertyChanged();
            }
        }

        public DateTime LastDateOpened
        {
            get => _preferences.Get(nameof(LastDateOpened), DateTime.Now.Date.AddDays(-1));
            set
            {
                var oldVal = LastDateOpened;

                _preferences.Set(nameof(LastDateOpened), value.Date);

                if (oldVal != LastDateOpened)
                    OnPropertyChanged();
            }
        }

        public int DayCount
        {
            get => _preferences.Get(nameof(DayCount), 0);
            set
            {
                var oldVal = DayCount;

                _preferences.Set(nameof(DayCount), value);

                if (oldVal != DayCount)
                    OnPropertyChanged();
            }
        }

        public bool ReviewRequested
        {
            get => _preferences.Get(nameof(ReviewRequested), false);
            set
            {
                var oldVal = ReviewRequested;

                _preferences.Set(nameof(ReviewRequested), value);

                if (oldVal != ReviewRequested)
                    OnPropertyChanged();
            }
        }
    }
}