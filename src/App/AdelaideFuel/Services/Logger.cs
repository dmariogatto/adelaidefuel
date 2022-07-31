using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace AdelaideFuel.Services
{
    public class Logger : ILogger
    {
        private const string LogFileName = "fuel_log.txt";
        
        private static readonly object LogLock = new object();

        private readonly IDeviceInfo _deviceInfo;
        private readonly string _logFilePath;

        public Logger(
            IDeviceInfo deviceInfo,
            IFileSystem fileSystem)
        {
            _deviceInfo = deviceInfo;

            _logFilePath = Path.Combine(fileSystem.CacheDirectory, LogFileName);
        }

        public void Error(Exception ex, IDictionary<string, string> data = null)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            WriteToLog(ex, string.Empty, data);
        }

        public void Event(string eventName, IDictionary<string, string> properties = null)
        {
            if (!string.IsNullOrWhiteSpace(eventName))
            {
                System.Diagnostics.Debug.WriteLine($"Tracking Event: {eventName}");
                if (_deviceInfo.DeviceType != DeviceType.Virtual)
                {
                    Analytics.TrackEvent(eventName, properties);
                }
            }
        }

        public long LogInBytes()
        {
            var size = 0L;

            if (File.Exists(_logFilePath))
            {
                try
                {
                    var fileInfo = new FileInfo(_logFilePath);
                    size = fileInfo.Length;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            return size;
        }

        public async Task<string> GetLog()
        {
            var result = string.Empty;

            if (File.Exists(_logFilePath))
            {
                try
                {
                    using var stream = File.OpenRead(_logFilePath);
                    using var reader = new StreamReader(stream);
                    result = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            return result;
        }

        public void DeleteLog()
        {
            if (File.Exists(_logFilePath))
            {
                try
                {
                    lock (LogLock)
                    {
                        File.Delete(_logFilePath);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }

        private void WriteToLog(Exception ex, string msg, IDictionary<string, string> data)
        {
            if (ex is not null &&
                ex.GetType() != typeof(TaskCanceledException) &&
                ex.GetType() != typeof(OperationCanceledException) &&
                _deviceInfo.DeviceType != DeviceType.Virtual)
            {
                Crashes.TrackError(ex, data);
            }

#if !DEBUG
            return;
#endif

            var logEntry = new List<string>
            {
                $"[{DateTime.UtcNow:o}]"
            };

            if (!string.IsNullOrEmpty(msg))
                logEntry.Add(msg);
            if (ex is not null)
                logEntry.Add(ex.ToString());
            if (data?.Any() == true)
                foreach (var d in data)
                    logEntry.Add($"{d.Key} : {d.Value}");

            lock (LogLock)
            {
                File.AppendAllLines(_logFilePath, logEntry);
            }
        }
    }
}