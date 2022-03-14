using System;
using System.ComponentModel;

namespace AdelaideFuel.Services
{
    public interface IAppPreferences : INotifyPropertyChanged
    {
        Theme AppTheme { get; set; }

        DateTime LastDateSynced { get; set; }
        DateTime LastDateOpened { get; set; }
        int DayCount { get; set; }
        bool ReviewRequested { get; set; }

        bool ShowRadiiOnMap { get; set; }
    }
}