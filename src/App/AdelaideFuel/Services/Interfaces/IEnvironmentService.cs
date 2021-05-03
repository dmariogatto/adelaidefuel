using System;

namespace AdelaideFuel.Services
{
    public interface IEnvironmentService
    {
        /// <summary>
        /// Whether device supports native dark mode
        /// </summary>
        bool NativeDarkMode { get; }

        /// <summary>
        /// Current OS theme
        /// </summary>
        Theme GetOperatingSystemTheme();
    }
}